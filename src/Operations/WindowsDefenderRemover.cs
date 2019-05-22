using Microsoft.Win32;
using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class WindowsDefenderRemover : IOperation
    {
        private static readonly string[] SECURITY_HEALTH_SERVICES = new[] { "Sense", "SecurityHealthService", "wscsvc" };

        public void PerformTask()
        {
            EditWindowsRegistryKeys();
            RemoveSecurityHealthServices();

            Console.WriteLine();
            OperationUtils.RemoveComponentUsingInstallWimTweak("Windows-Defender");
        }

        private void EditWindowsRegistryKeys()
        {
            Console.WriteLine("Editing keys in Windows Registry...");

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"))
                key.SetValue("SmartScreenEnabled", "Off", RegistryValueKind.String);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost"))
                key.SetValue("EnableWebContentEvaluation", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Local Settings\Software\Microsoft\" +
                   @"Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\PhishingFilter"))
                key.SetValue("EnabledV9", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet"))
            {
                key.SetValue("SpyNetReporting", 0, RegistryValueKind.DWord);
                key.SetValue("SubmitSamplesConsent", 2, RegistryValueKind.DWord);
                key.SetValue("DontReportInfectionInformation", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\MRT"))
            {
                key.SetValue("DontReportInfectionInformation", 1, RegistryValueKind.DWord);
                key.SetValue("DontOfferThroughWUAU", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                key.DeleteValue("SecurityHealth", false);

            // It seems that this key can't be retrieved inside this program, neither via .NET API nor via CMD (permissions needed? API bug?)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true))
            {
                if (key != null)
                    key.DeleteValue("SecurityHealth", false);
                else
                    ConsoleUtils.WriteLine(@"Remember to execute manually command ""reg delete HKLM\SOFTWARE\Microsoft\Windows\" +
                                           @"CurrentVersion\Explorer\StartupApproved\Run /v SecurityHealth /f""", ConsoleColor.DarkYellow);
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\" +
                   @"Image File Execution Options\SecHealthUI.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
        }

        private void RemoveSecurityHealthServices()
        {
            Console.WriteLine("\nRemoving Security Health services...");

            new ServiceRemover(SECURITY_HEALTH_SERVICES)
                .PerformBackup()
                .PerformRemoval(ServiceRemovalMode.Registry);
        }
    }
}
