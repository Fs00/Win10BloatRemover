using System;
using System.Diagnostics;
using System.IO;
using System.Resources;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    /**
     *  Utils
     *  Contains functions that perform tasks which don't belong to a particular category
     */
    static class Utils
    {
        public static void ExtractInstallWimTweak()
        {
            ResourceManager binResources = new ResourceManager("Win10BloatRemover.resources.Binaries", typeof(Utils).Assembly);
            File.WriteAllBytes(Configuration.InstallWimTweakPath, (byte[]) binResources.GetObject("install_wim_tweak"));
        }

        public static Process RunInstallWimTweak(string arguments)
        {
            var installWimTweakProcess = new Process();
            installWimTweakProcess.StartInfo = new ProcessStartInfo {
                FileName = Configuration.InstallWimTweakPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            installWimTweakProcess.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
            installWimTweakProcess.ErrorDataReceived += (s, e) => {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Data);
                Console.ResetColor();
            };
            installWimTweakProcess.Start();
            installWimTweakProcess.BeginOutputReadLine();
            installWimTweakProcess.BeginErrorReadLine();
            return installWimTweakProcess;
        }

        public static void DeleteTempInstallWimTweak()
        {
            if (File.Exists(Configuration.InstallWimTweakPath))
                File.Delete(Configuration.InstallWimTweakPath);
        }

        public static void ExecuteWindowsCommand(string command)
        {
            var cmdProcess = new Process();
            cmdProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            cmdProcess.Start();
            Console.Write(cmdProcess.StandardOutput.ReadToEnd());
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(cmdProcess.StandardError.ReadToEnd());
            Console.ResetColor();
            cmdProcess.WaitForExit();
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
            // SEEMS NOT TO WORK AS INTENDED, MORE INVESTIGATIONS TO BE DONE
            using (RegistryKey winUpdatePolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                winUpdatePolicies.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
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

            // It seems that this key can't be retrieved programmatically (only MS knows why)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true))
            {
                if (key != null)
                    key.DeleteValue("SecurityHealth", false);
                Console.WriteLine("WARNING: Remember to execute manually command \"reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run\" /v \"SecurityHealth\" /f\"");
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SecHealthUI.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);

            Console.WriteLine("OK!");
            
            Console.WriteLine("Running install-wim-tweak...");
            var installWimTweakProc = RunInstallWimTweak("/o /c Windows-Defender /r");
            installWimTweakProc.WaitForExit();
            if (installWimTweakProc.ExitCode == 0)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred during the execution of install-wim-tweak: non-zero exit status.");
                Console.ResetColor();
            }
        }

        public static void RemoveMicrosoftEdge()
        {
            Console.WriteLine("Remember to unpin Edge from your taskbar, otherwise you won't be able to do it!");
            Console.WriteLine("Press a key when you're ready.");
            Console.ReadKey();

            Console.WriteLine("Running install-wim-tweak...");
            var installWimTweakProc = RunInstallWimTweak("/o /c Microsoft-Windows-Internet-Browser /r");
            installWimTweakProc.WaitForExit();
            if (installWimTweakProc.ExitCode == 0)
                Console.WriteLine("Install-wim-tweak executed successfully!");
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred during the execution of install-wim-tweak: non-zero exit status.");
                Console.ResetColor();
            }
        }

        public static void DisableScheduledTasks(string[] scheduledTasksList)
        {
            foreach (string task in scheduledTasksList)
                ExecuteWindowsCommand($"schtasks /Change /TN \"{task}\" /disable");

            ExecuteWindowsCommand("del /F /Q \"C:\\Windows\\System32\\Tasks\\Microsoft\\Windows\\SettingSync\\*\"");
            Console.WriteLine("Some commands may fail, it's normal.");
        }
    }
}
