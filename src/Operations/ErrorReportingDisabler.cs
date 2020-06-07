using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class ErrorReportingDisabler : IOperation
    {
        private static readonly string[] errorReportingServices = { "WerSvc", "wercplsupport" };
        private static readonly string[] errorReportingScheduledTasks = {
            @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"
        };

        private readonly IUserInterface ui;
        public ErrorReportingDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableErrorReporting();
            RemoveErrorReportingServices();
            DisableErrorReportingScheduledTasks();
        }
        
        private void DisableErrorReporting()
        {
            ui.PrintHeading("Writing values into the Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
        }

        private void RemoveErrorReportingServices()
        {
            ui.PrintHeading("Backing up and removing error reporting services...");
            ServiceRemover.BackupAndRemove(errorReportingServices, ui);
        }
        
        private void DisableErrorReportingScheduledTasks()
        {
            ui.PrintHeading("Disabling error reporting scheduled tasks...");
            new ScheduledTasksDisabler(errorReportingScheduledTasks, ui).Run();
        }
    }
}
