using Microsoft.Win32;
using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class WindowsTipsDisabler : IOperation
    {
        public void PerformTask()
        {
            ConsoleUtils.WriteLine("Writing values into the Registry...", ConsoleColor.Green);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
            {
                key.SetValue("DisableSoftLanding", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                key.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace"))
                key.SetValue("AllowSuggestedAppsInWindowsInkWorkspace", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Siuf\Rules"))
                key.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);

            ConsoleUtils.WriteLine("\nDisabling feedback-related scheduled tasks...", ConsoleColor.Green);
            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"
            }).PerformTask();
        }
    }
}
