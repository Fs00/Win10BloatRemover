using Microsoft.Win32;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class WindowsDefenderRemover : IOperation
    {
        private static readonly string[] securityHealthServices = {
            "SecurityHealthService",
            "wscsvc",
            "Sense",
            "SgrmBroker",
            "SgrmAgent"
        };

        private readonly InstallWimTweak installWimTweak;
        private readonly IUserInterface ui;
        private readonly IOperation securityCenterRemover;

        public WindowsDefenderRemover(IUserInterface ui, InstallWimTweak installWimTweak, IOperation securityCenterRemover)
        {
            this.ui = ui;
            this.installWimTweak = installWimTweak;
            this.securityCenterRemover = securityCenterRemover;
        }

        public void Run()
        {
            EditWindowsRegistryKeys();
            RemoveSecurityHealthServices();

            ui.PrintEmptySpace();
            installWimTweak.RemoveComponentIfAllowed("Windows-Defender", ui);

            securityCenterRemover.Run();
        }

        private void EditWindowsRegistryKeys()
        {
            ui.PrintHeading("Editing keys in Windows Registry...");

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
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true))
                key.DeleteValue("SecurityHealth", throwOnMissingValue: false);

            using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using RegistryKey key = localMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", writable: true
                );
                key.DeleteValue("SecurityHealth", throwOnMissingValue: false);
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\" +
                   @"Image File Execution Options\SecHealthUI.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
        }

        private void RemoveSecurityHealthServices()
        {
            ui.PrintHeading("\nRemoving Security Health services...");
            ServiceRemover.BackupAndRemove(securityHealthServices, ui, ServiceRemovalMode.Registry);
        }
    }
}
