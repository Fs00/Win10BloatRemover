using System;
using System.IO;
using Microsoft.Win32;
using System.Management.Automation;

namespace Win10BloatRemover
{
    /**
     *  Operations
     *  Contains functions that perform tasks which don't belong to a particular category
     */
    static class Operations
    {
        /**
         *  Removes the specified component using install-wim-tweak synchronously
         *  It also prints output messages according to its exit status
         *  Messages from install-wim-tweak process are printed asynchronously (as soon as they are written to stdout/stderr)
         */
        public static void RemoveComponentUsingInstallWimTweak(string component)
        {
            Console.WriteLine($"Running install-wim-tweak to remove {component}...");
            using (var installWimTweakProcess = SystemUtils.RunProcess(Program.InstallWimTweakPath, $"/o /c {component} /r", true))
            {
                installWimTweakProcess.BeginOutputReadLine();
                installWimTweakProcess.BeginErrorReadLine();
                installWimTweakProcess.WaitForExit();
                if (installWimTweakProcess.ExitCode == 0)
                    Console.WriteLine("Install-wim-tweak executed successfully!");
                else
                    ConsoleUtils.WriteLine($"An error occurred during the removal of {component}: install-wim-tweak exited with a non-zero status.", ConsoleColor.Red);
            }
        }

        public static void DisableCortana()
        {
            // Set group policy to disable Cortana
            using (RegistryKey winSearchPolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                winSearchPolicies.SetValue("AllowCortana", 0, RegistryValueKind.DWord);

            // Add firewall rule to prevent Cortana connecting to Internet
            using (RegistryKey firewallRules = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"))
                firewallRules.SetValue("{2765E0F4-2918-4A46-B9C9-43CDD8FCBA2B}", "BlockCortana|Action=Block|Active=TRUE|Dir=Out|" +
                                        @"App=C:\windows\systemapps\microsoft.windows.cortana_cw5n1h2txyewy\searchui.exe|Name=Search and Cortana application|" +
                                        "AppPkgId=S-1-15-2-1861897761-1695161497-2927542615-642690995-327840285-2659745135-2630312742|", RegistryValueKind.String);
        }

        public static void DisableAutomaticUpdates()
        {
            // Disable Windows Update auto updates
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);

            // Disable Windows Store auto updates
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore"))
                key.SetValue("AutoDownload", 2, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\WindowsStore"))
                key.SetValue("AutoDownload", 2, RegistryValueKind.DWord);
        }

        public static void RemoveWindowsDefender()
        {
            Console.WriteLine("Editing keys in Windows Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                key.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost"))
                key.SetValue("EnableWebContentEvaluation", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\PhishingFilter"))
                key.SetValue("EnabledV9", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet"))
            {
                key.SetValue("SpyNetReporting", 0, RegistryValueKind.DWord);
                key.SetValue("SubmitSamplesConsent", 2, RegistryValueKind.DWord);
                key.SetValue("DontReportInfectionInformation", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true))
            {
                key.DeleteSubKeyTree("Sense", false);
                key.DeleteSubKeyTree("SecurityHealthService", false);
                key.DeleteSubKeyTree("wscsvc", false);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\MRT"))
            {
                key.SetValue("DontReportInfectionInformation", 1, RegistryValueKind.DWord);
                key.SetValue("DontOfferThroughWUAU", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                key.DeleteValue("SecurityHealth", false);

            // It seems that this key can't be retrieved programmatically (API bug?)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true))
            {
                if (key != null)
                    key.DeleteValue("SecurityHealth", false);
                else
                    ConsoleUtils.WriteLine("WARNING: Remember to execute manually command \"reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run\"" +
                                           " /v \"SecurityHealth\" /f\"", ConsoleColor.DarkYellow);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SecHealthUI.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);

            Console.WriteLine();
            RemoveComponentUsingInstallWimTweak("Windows-Defender");
        }

        public static void RemoveOneDrive()
        {
            Console.WriteLine("Killing OneDrive process...");
            SystemUtils.ExecuteWindowsCommand("taskkill /F /IM onedrive.exe");

            Console.WriteLine("Executing OneDrive uninstaller...");
            string oneDriveUninstaller = RetrieveOneDriveUninstallerPath();
            using (var oneDriveSetupProc = SystemUtils.RunProcess(oneDriveUninstaller, "/uninstall"))
            {
                oneDriveSetupProc.PrintOutputAndErrors();
                oneDriveSetupProc.WaitForExit();

                if (oneDriveSetupProc.ExitCode != 0)
                    throw new Exception("OneDrive uninstaller terminated with non-zero status.");
                else
                {
                    Console.WriteLine("Removing old files...");
                    SystemUtils.DeleteDirectoryIfExists($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\OneDrive", handleErrors: true);
                    SystemUtils.DeleteDirectoryIfExists(@"C:\OneDriveTemp", handleErrors: true);
                    SystemUtils.DeleteDirectoryIfExists($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", handleErrors: true);
                    SystemUtils.DeleteDirectoryIfExists($@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", handleErrors: true);

                    try
                    {
                        string oneDriveStandaloneUpdater = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive\OneDriveStandaloneUpdater.exe";
                        if (File.Exists(oneDriveStandaloneUpdater))
                            File.Delete(oneDriveStandaloneUpdater);
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"An error occurred while deleting OneDrive standalone updater: {exc.Message}");
                    }

                    Console.WriteLine("Deleting old registry keys...");
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID", true))
                        key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID", true))
                        key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
                }
            }
        }

        private static string RetrieveOneDriveUninstallerPath()
        {
            if (Environment.Is64BitOperatingSystem)
                return $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe";
            else
                return $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\System32\OneDriveSetup.exe";
        }

        public static void DisableScheduledTasks(string[] scheduledTasksList)
        {
            foreach (string task in scheduledTasksList)
                SystemUtils.ExecuteWindowsCommand($"schtasks /Change /TN \"{task}\" /disable");

            SystemUtils.ExecuteWindowsCommand("del /F /Q \"C:\\Windows\\System32\\Tasks\\Microsoft\\Windows\\SettingSync\\*\"");
        }

        public static void DisableWinErrorReporting()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
        }

        public static void DisableWindowsTipsAndFeedback()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
            {
                key.SetValue("DisableSoftLanding", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                key.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace"))
                key.SetValue("AllowSuggestedAppsInWindowsInkWorkspace", 0, RegistryValueKind.DWord);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Siuf\Rules"))
                key.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
        }

        public static void RemoveWindowsFeatures(string[] featuresToRemove)
        {
            string removalScript = "";
            foreach (string featureName in featuresToRemove)
            {
                removalScript += $"$feature = Get-WindowsPackage -Online -PackageName *{featureName}*;" +
                                 "if ($feature) {" +
                                    "Write-Host \"Removing feature $($feature.PackageName)...\";" +
                                    "Remove-WindowsPackage -Online -NoRestart -PackageName $feature.PackageName;" +
                                 "}" +
                                 "else" +
                                    "{ Write-Host \"Feature " + featureName + " is not installed.\"; }";

                if (featureName == "Hello-Face-Package")
                    removalScript += "schtasks /Change /TN \"\\Microsoft\\Windows\\HelloFace\\FODCleanupTask\" /Disable;";
            }

            using (PowerShell psInstance = PowerShell.Create())
                psInstance.RunScriptAndPrintOutput(removalScript);
        }

        /**
         *  Additional tasks to disable telemetry-related features
         *  Include blocking of CompatTelRunner, Inventory (collection of installed programs), Steps Recorder, Compatibility Assistant
         */
        public static void DisableTelemetryRelatedFeatures()
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
