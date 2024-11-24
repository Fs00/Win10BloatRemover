using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations;

class ScheduledTasksDisabler(string[] scheduledTasksToDisable, IUserInterface ui) : IOperation
{
    public void Run()
    {
        foreach (string task in scheduledTasksToDisable)
        {
            OS.RunProcessBlockingWithOutput(
                OS.SystemExecutablePath("schtasks"), $@"/Change /TN ""{task}"" /disable",
                ui
            );
        }
    }
}
