using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class ScheduledTasksDisabler : IOperation
    {
        public void PerformTask()
        {
            foreach (string task in Configuration.Instance.ScheduledTasksToDisable)
                SystemUtils.ExecuteWindowsCommand($@"schtasks /Change /TN ""{task}"" /disable");

            SystemUtils.ExecuteWindowsCommand(@"del /F /Q ""C:\Windows\System32\Tasks\Microsoft\Windows\SettingSync\*""");
            Console.WriteLine("Some commands may fail, it's normal.");
        }
    }
}
