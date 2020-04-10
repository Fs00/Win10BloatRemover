using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    class TipsDisabler : IOperation
    {
        private readonly IUserInterface ui;
        public TipsDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            ui.PrintHeading("Writing values into the Registry...");
            EditRegistryKeysToDisableTips();

            ui.PrintHeading("\nDisabling feedback-related scheduled tasks...");
            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"
            }, ui).Run();
        }

        private void EditRegistryKeysToDisableTips()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CloudContent"))
            {
                key.SetValue("DisableSoftLanding", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord);
                key.SetValue("DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord);
                key.SetValue("DisableTailoredExperiencesWithDiagnosticData", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                key.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsInkWorkspace"))
                key.SetValue("AllowSuggestedAppsInWindowsInkWorkspace", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Siuf\Rules"))
                key.SetValue("NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord);
        }
    }
}
