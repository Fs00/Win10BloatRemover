using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class ScheduledTasksDisabler : IOperation
    {
        private readonly string[] scheduledTasksToDisable;
        private readonly IUserInterface ui;

        public ScheduledTasksDisabler(string[] scheduledTasksToDisable, IUserInterface ui)
        {
            this.ui = ui;
            this.scheduledTasksToDisable = scheduledTasksToDisable;
        }

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
}
