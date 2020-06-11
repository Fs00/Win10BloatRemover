using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class TipsDisabler : IOperation
    {
        private readonly IUserInterface ui;
        public TipsDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableTips();

            ui.PrintHeading("Disabling feedback-related scheduled tasks...");
            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"
            }, ui).Run();
        }

        private void DisableTips()
        {
            ui.PrintHeading("Writing values into the Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
            {
                key.SetValue("DisableSoftLanding", 1);
                key.SetValue("DisableWindowsSpotlightFeatures", 1);
                key.SetValue("DisableWindowsConsumerFeatures", 1);
                key.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1);
            }
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "DoNotShowFeedbackNotifications", 1
            );
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsInkWorkspace",
                "AllowSuggestedAppsInWindowsInkWorkspace", 0
            );
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0);
        }
    }
}
