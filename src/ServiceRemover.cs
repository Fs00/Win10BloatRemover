using System;
using System.IO;
using System.Management.Automation;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    /**
     *  ServiceRemover
     *  Performs backup (export of registry keys) and removal of the services passed into the constructor
     */
    class ServiceRemover
    {
        private readonly string[] servicesToRemove;
        private bool backupPerformed,
                     removalPerformed;

        public ServiceRemover(string[] servicesToRemove)
        {
            this.servicesToRemove = servicesToRemove;
        }

        public ServiceRemover PerformBackup()
        {
            if (backupPerformed)
                throw new InvalidOperationException("Backup already done!");

            DirectoryInfo backupDirectory = Directory.CreateDirectory($"./servicesBackup_{DateTime.Now.ToString("yyyy-MM-dd_hh-mm")}");
            foreach (string serviceName in servicesToRemove)
            {
                // Here we find all the services that start with the specified service name, in order to include services that end with a random code
                // Destination file will have the name of the service
                using (PowerShell psInstance = PowerShell.Create())
                {
                    string backupScript = "$services = Get-ChildItem -Path HKLM:\\SYSTEM\\CurrentControlSet\\Services -Name |" +
                                                       "Where-Object {$_ -Match \"^" + serviceName + "\"};" +
                                          "if ($services) {" +
                                              "foreach ($serviceName in $services) {" +
                                                   $"reg export HKLM\\SYSTEM\\CurrentControlSet\\Services\\$serviceName {backupDirectory.FullName}\\$($serviceName).reg;" +
                                                    "if ($LASTEXITCODE -ne 0)" +
                                                        "{ Write-Error \"Backup failed for service $serviceName.\" }" +
                                                    "else" +
                                                        "{ Write-Host \"Service $serviceName backed up.\" }" +
                                              "}" +
                                          "}" +
                                          "else { Write-Host \"No services found with name " + serviceName + "\" }";

                    psInstance.RunScriptAndPrintOutput(backupScript);
                }
            }

            backupPerformed = true;
            return this;    // allows chaining with PerformRemoval
        }

        public void PerformRemoval()
        {
            if (!backupPerformed)
                throw new InvalidOperationException("Backup services before removing them!");
            if (removalPerformed)
                throw new InvalidOperationException("Removal already performed.");

            foreach (string serviceName in servicesToRemove)
            {
                // PowerShell session is recreated every time to prevent single output messages
                // being written multiple times (which is likely a bug in the API)
                using (PowerShell psInstance = PowerShell.Create())
                {
                    string removalScript = "$services = Get-ChildItem -Path HKLM:\\SYSTEM\\CurrentControlSet\\Services -Name |" +
                                                        "Where-Object {$_ -Match \"^" + serviceName + "\"};" +
                                            "foreach ($service in $services) {" +
                                                "sc.exe delete $service;" +
                                                "if ($LASTEXITCODE -eq 0)" +
                                                    "{ Write-Host \"Service $service removed successfully.\" }" +
                                                "else" +
                                                    "{ Write-Error \"Service $service removal failed: exit code $LASTEXITCODE\" }" +
                                                /*
                                                 * Here I tried to edit permissions for service reg keys which are protected by permissions.
                                                 * Unfortunately the subkeys of those services can't be opened even with ChangePermissions permission.
                                                 */
                                                /*
                                                "if ($LASTEXITCODE = 5) {" +
                                                    "Write-Host \"Access denied, editing key permissions\";" +
                                                    "$serviceKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey(\"SYSTEM\\CurrentControlSet\\Services\\$service\"," +
                                                                    "[Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree," +
                                                                    "[System.Security.AccessControl.RegistryRights]::ChangePermissions);" +
                                                    "$acl = $serviceKey.GetAccessControl();" +
                                                    "$acl.GetAccessRules(1, 1, [type][System.Security.Principal.NTAccount]) | foreach { $acl.RemoveAccessRule($service) };" +
                                                    "$currentUser = [System.Security.Principal.NTAccount]\"$env:userdomain\\$env:username\";" +
                                                    "$newRule = New-Object System.Security.AccessControl.RegistryAccessRule($currentUser," +
                                                                "[System.Security.AccessControl.RegistryRights]\"FullControl\"," +
                                                                "[System.Security.AccessControl.InheritanceFlags]\"ContainerInherit, ObjectInherit\"," +
                                                                "[System.Security.AccessControl.PropagationFlags]\"None\"," +
                                                                "[System.Security.AccessControl.AccessControlType]\"Allow\");" +
                                                    "$acl.AddAccessRule($newRule);" +
                                                    "$acl.SetAccessRuleProtection(1, 0);" +
                                                    "$serviceKey.SetAccessControl($acl);" +
                                                    "$serviceSubkeys = Get-ChildItem -Path HKLM:\\SYSTEM\\CurrentControlSet\\Services\\$service -Name;" +
                                                    "foreach ($serviceSubkey in $serviceSubkeys) {" +
                                                        "$subkey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey(\"SYSTEM\\CurrentControlSet\\Services\\$service\\$serviceSubkey\"," +
                                                                    "[Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree," +
                                                                    "[System.Security.AccessControl.RegistryRights]::ChangePermissions);" +
                                                        "subkey.SetAccessControl($acl);" +
                                                    "}" +
                                                    "sc.exe delete $service;" +
                                                    "if ($LASTEXITCODE = 0) { Write-Host \"Removal successful\" }" +
                                                    "else { Write-Error \"Removal failed after changing permissions\" }" +
                                                "}" +*/
                                            "}";

                    psInstance.RunScriptAndPrintOutput(removalScript);
                }
            }

            Console.WriteLine("Performing additional tasks to disable telemetry-related features...");
            PerformAdditionalTasks();
            removalPerformed = true;
        }

        /**
         *  Additional tasks to disable telemetry-related features
         *  Include blocking of CompatTelRunner, Inventory (collection of installed programs), Steps Recorder, Compatibility Assistant
         */
        private void PerformAdditionalTasks()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener"))
                key.SetValue("Start", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
            {
                key.SetValue("AITEnable", 0, RegistryValueKind.DWord);
                key.SetValue("DisableInventory", 1, RegistryValueKind.DWord);
                key.SetValue("DisablePCA", 1, RegistryValueKind.DWord);
                key.SetValue("DisableUAR", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                key.SetValue("EnableSmartScreen", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
        }
    }
}
