using Microsoft.Win32;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class DefenderDisabler : IOperation
    {
        private static readonly string[] securityHealthServices = {
            "SecurityHealthService",
            "wscsvc",
            "Sense",
            "SgrmBroker",
            "SgrmAgent"
        };

        private readonly IUserInterface ui;
        private readonly IOperation securityCenterRemover;

        public DefenderDisabler(IUserInterface ui, IOperation securityCenterRemover)
        {
            this.ui = ui;
            this.securityCenterRemover = securityCenterRemover;
        }

        public void Run()
        {
            EditWindowsRegistryKeys();
            RemoveSecurityHealthServices();
            securityCenterRemover.Run();
        }

        private void EditWindowsRegistryKeys()
        {
            ui.PrintHeading("Editing keys in Windows Registry...");

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0);
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", "EnabledV9", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet"))
            {
                key.SetValue("SpynetReporting", 0);
                key.SetValue("SubmitSamplesConsent", 2);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\MRT"))
            {
                key.SetValue("DontReportInfectionInformation", 1);
                key.SetValue("DontOfferThroughWUAU", 1);
            }

            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localMachine.DeleteSubKeyValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth");
            localMachine.DeleteSubKeyValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "SecurityHealth");
        }

        private void RemoveSecurityHealthServices()
        {
            ui.PrintHeading("Removing Security Health services...");
            ServiceRemover.BackupAndRemove(securityHealthServices, ui);
        }
    }
}
