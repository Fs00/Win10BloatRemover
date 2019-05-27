using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class ScheduledTasksDisabler : IOperation
    {
        private readonly string[] scheduledTasksToDisable;

        public ScheduledTasksDisabler(string[] scheduledTasksToDisable)
        {
            this.scheduledTasksToDisable = scheduledTasksToDisable;
        }

        public void PerformTask()
        {
            foreach (string task in scheduledTasksToDisable)
                ShellUtils.ExecuteWindowsCommand($@"schtasks /Change /TN ""{task}"" /disable");
        }
    }
}
