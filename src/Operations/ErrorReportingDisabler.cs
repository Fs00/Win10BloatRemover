using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class ErrorReportingDisabler : IOperation
    {
        private static readonly string[] errorReportingServices = { "WerSvc", "wercplsupport" };

        private readonly IUserInterface ui;
        public ErrorReportingDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableErrorReporting();

            ui.PrintHeading("Backing up and removing error reporting services...");
            ServiceRemover.BackupAndRemove(errorReportingServices, ui);
        }

        private void DisableErrorReporting()
        {
            ui.PrintHeading("Writing values into the Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
        }
    }
}
