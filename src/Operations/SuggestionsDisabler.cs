using Microsoft.Win32;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class SuggestionsDisabler : IOperation
    {
        private readonly IUserInterface ui;
        public SuggestionsDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableSuggestions();
            DisableCloudContent();
            DisableFeedbackRequests();
        }

        private void DisableSuggestions()
        {
            ui.PrintHeading("Disabling suggestions via Registry edits...");

            // System -> Notifications & actions -> Show the Windows welcome experience...
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0);
            // System -> Notifications & actions -> Get tips, tricks, and suggestions as you use Windows
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0);
            // Personalization -> Start -> Show suggestions occasionally in Start
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0);
            // Privacy -> General -> Show suggested content in Settings app
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0);
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0);
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353696Enabled", 0);
            // System -> Notifications & actions -> Suggest ways I can finish setting up my device...
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement",
                "ScoobeSystemSettingEnabled", 0);

            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsInkWorkspace",
                "AllowSuggestedAppsInWindowsInkWorkspace", 0);
            // Disables online tips and help for Settings app
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "AllowOnlineTips", 0);
        }

        private void DisableCloudContent()
        {
            ui.PrintHeading("Disabling Spotlight, News and Interests and other cloud content via Registry edits...");

            // These two policies work only on Education and Enterprise editions
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", 1);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);

            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Policies\Microsoft\Windows\CloudContent",
                "DisableWindowsSpotlightFeatures", 1);
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"Software\Policies\Microsoft\Windows\CloudContent",
                "DisableTailoredExperiencesWithDiagnosticData", 1);

            // News and Interests
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0);
            // Programmable taskbar
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableCloudOptimizedContent", 1);
        }

        private void DisableFeedbackRequests()
        {
            ui.PrintHeading("Disabling feedback requests and related scheduled tasks...");
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "DoNotShowFeedbackNotifications", 1);
            RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0);

            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"
            }, ui).Run();
        }
    }
}
