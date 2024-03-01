using Microsoft.Win32;
using Win10BloatRemover.UI;

namespace Win10BloatRemover.Operations;

public class ErrorReportingDisabler(IUserInterface ui, ServiceRemover serviceRemover) : IOperation
{
    private static readonly string[] errorReportingServices = ["WerSvc", "wercplsupport"];
    private static readonly string[] errorReportingScheduledTasks = [
        @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"
    ];

    public bool IsRebootRecommended => serviceRemover.IsRebootRecommended;

    public void Run()
    {
        DisableErrorReporting();
        RemoveErrorReportingServices();
        DisableErrorReportingScheduledTasks();
    }
    
    private void DisableErrorReporting()
    {
        ui.PrintHeading("Writing values into the Registry...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\PCHealth\ErrorReporting", "DoReport", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1);
    }

    private void RemoveErrorReportingServices()
    {
        ui.PrintHeading("Backing up and removing error reporting services...");
        serviceRemover.BackupAndRemove(errorReportingServices);
    }
    
    private void DisableErrorReportingScheduledTasks()
    {
        ui.PrintHeading("Disabling error reporting scheduled tasks...");
        new ScheduledTasksDisabler(errorReportingScheduledTasks, ui).Run();
    }
}
