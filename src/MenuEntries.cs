using System;
using System.Linq;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    abstract class MenuEntry
    {
        public abstract string FullName { get; }
        public virtual bool ShouldQuit => false;
        public abstract string GetExplanation();
        public abstract IOperation CreateNewOperation(IUserInterface ui);
    }

    class SystemAppsRemovalEnablingEntry : MenuEntry
    {
        public override string FullName => "Make system apps removable";
        public override string GetExplanation()
        {
            return
@"This procedure will edit an internal database to allow the removal of system UWP apps such as legacy Edge and
Security Center via PowerShell (used by this tool) and in Settings app.
It is recommended to create a system restore point before proceeding.

It is generally safe to remove only those system apps that can be found in Start menu.
Certain ""hidden"" apps are there to provide critical OS functionality, and therefore uninstalling them may lead
to an unstable or unusable system: BE CAREFUL.

Remember also that any system app may be reinstalled after any Windows cumulative update.
Before starting, make sure that Microsoft Store is not installing/updating apps in the background.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new SystemAppsRemovalEnabler(ui);
    }

    class UWPAppRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public UWPAppRemovalEntry(Configuration configuration) => this.configuration = configuration;

        public override string FullName => "Remove UWP apps";
        public override string GetExplanation()
        {
            string impactedUsers = configuration.UWPAppsRemovalMode == UWPAppRemovalMode.CurrentUser
                ? "the current user"
                : "all present and future users";
            string explanation = $"The following groups of UWP apps will be removed for {impactedUsers}:\n";
            foreach (UWPAppGroup app in configuration.UWPAppsToRemove)
                explanation += $"  {app}\n";

            if (configuration.UWPAppsRemovalMode == UWPAppRemovalMode.AllUsers)
                explanation += "\nServices, components and scheduled tasks used specifically by those apps will also " +
                               "be disabled or removed,\ntogether with any leftover data.";

            if (configuration.UWPAppsToRemove.Contains(UWPAppGroup.Xbox))
                explanation += "\n\nIn order to fully remove Xbox apps, you need to make system apps removable first.";

            return explanation;
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new UWPAppRemover(configuration.UWPAppsToRemove, configuration.UWPAppsRemovalMode, ui, new ServiceRemover(ui));
    }

    class DefenderDisablingEntry : MenuEntry
    {
        public override string FullName => "Disable Windows Defender antivirus";
        public override string GetExplanation()
        {
            return
@"IMPORTANT: Before starting, disable Tamper protection in Windows Security app under Virus & threat protection settings.

Windows Defender antimalware engine and SmartScreen feature will be disabled via Group Policies, and services
related to those features will be removed.
Furthermore, Windows Security app will be prevented from running automatically at system start-up.

Windows Defender Firewall will continue to work as intended.";
        }

        public override IOperation CreateNewOperation(IUserInterface ui)
        {
            return new DefenderDisabler(ui, new ServiceRemover(ui));
        }
    }

    class EdgeRemovalEntry : MenuEntry
    {
        public override string FullName => "Remove Microsoft Edge";
        public override string GetExplanation()
        {
            return
@"Both Edge Chromium and legacy Edge browser will be uninstalled from the system.
In order to be able to uninstall the latter (which may appear in Start menu once you uninstall the former),
you need to make system apps removable.
Take note that both browsers may be reinstalled after any Windows cumulative update.
Make sure that Edge Chromium is not updating itself before proceeding.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
        {
            return new EdgeRemover(ui,
                new UWPAppRemover(
                    new[] { UWPAppGroup.EdgeUWP },
                    UWPAppRemovalMode.AllUsers,
                    ui, new ServiceRemover(ui)
                )
            );
        }
    }

    class OneDriveRemovalEntry : MenuEntry
    {
        public override string FullName => "Remove OneDrive";
        public override string GetExplanation()
        {
            return "OneDrive will be disabled using Group Policies and then uninstalled for the current user.\n" +
                   "Futhermore, it will be prevented from being installed when a new user logs in for the first time.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new OneDriveRemover(ui);
    }

    class ServicesRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public ServicesRemovalEntry(Configuration configuration) => this.configuration = configuration;

        public override string FullName => "Remove miscellaneous services";
        public override string GetExplanation()
        {
            string explanation = "The services starting with the following names will be removed:\n";
            foreach (string service in configuration.ServicesToRemove)
                explanation += $"  {service}\n";
            return explanation + "Services will be backed up in the same folder as this program executable.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new ServiceRemovalOperation(configuration.ServicesToRemove, ui, new ServiceRemover(ui));
    }

    class WindowsFeaturesRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public WindowsFeaturesRemovalEntry(Configuration configuration) => this.configuration = configuration;

        public override string FullName => "Remove Windows features";
        public override string GetExplanation()
        {
            string explanation = "The following features on demand will be removed:";
            foreach (string feature in configuration.WindowsFeaturesToRemove)
                explanation += $"\n  {feature}";
            return explanation;
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new FeaturesRemover(configuration.WindowsFeaturesToRemove, ui);
    }

    class PrivacySettingsTweakEntry : MenuEntry
    {
        public override string FullName => "Tweak settings for privacy";
        public override string GetExplanation()
        {
            return
@"Several default settings and policies will be changed to make Windows more respectful of user's privacy.
These changes consist essentially of:
  - adjusting various options under Privacy section of Settings app (disable advertising ID, app launch tracking etc.)
  - preventing input data (inking/typing information, speech) from being sent to Microsoft to improve their services
  - preventing Edge from sending browsing history, favorites and other data to Microsoft in order to personalize ads,
    news and other services for your Microsoft account
  - denying access to sensitive data (location, documents, activities, account details, diagnostic info) to
    all UWP apps by default
  - disabling voice activation for voice assistants (so that they can't always be listening)
  - disabling cloud synchronization of sensitive data (user activities, clipboard, text messages, passwords
    and app data)
  - disabling web search in bottom search bar

Whereas almost all of these settings are applied for all users, some of them will only be changed for the current
user and for new users created after running this procedure.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new PrivacySettingsTweaker(ui);
    }

    class TelemetryDisablingEntry : MenuEntry
    {
        public override string FullName => "Disable telemetry";
        public override string GetExplanation()
        {
            return
@"This procedure will disable scheduled tasks, services and features that are responsible for collecting and
reporting data to Microsoft, including Compatibility Telemetry, Device Census, Customer Experience Improvement
Program and Compatibility Assistant.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new TelemetryDisabler(ui, new ServiceRemover(ui));
    }

    class AutoUpdatesDisablingEntry : MenuEntry
    {
        public override string FullName => "Disable automatic updates";
        public override string GetExplanation()
        {
            return "Automatic updates for Windows, Store apps and speech models will be disabled using Group Policies.\n" + 
                   "At least Windows 10 Pro edition is required to disable automatic Windows updates.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new AutoUpdatesDisabler(ui);
    }

    class ScheduledTasksDisablingEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public ScheduledTasksDisablingEntry(Configuration configuration) => this.configuration = configuration;

        public override string FullName => "Disable miscellaneous scheduled tasks";
        public override string GetExplanation()
        {
            string explanation = "The following scheduled tasks will be disabled:";
            foreach (string task in configuration.ScheduledTasksToDisable)
                explanation += $"\n  {task}";
            return explanation;
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new ScheduledTasksDisabler(configuration.ScheduledTasksToDisable, ui);
    }

    class ErrorReportingDisablingEntry : MenuEntry
    {
        public override string FullName => "Disable Windows Error Reporting";
        public override string GetExplanation()
        {
            return
@"Windows Error Reporting will disabled by editing Group Policies, as well as by removing its services (after
backing them up).";
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new ErrorReportingDisabler(ui, new ServiceRemover(ui));
    }

    class SuggestionsDisablingEntry : MenuEntry
    {
        public override string FullName => "Disable suggestions, cloud content and feedback requests";
        public override string GetExplanation()
        {
            return 
@"Feedback notifications and requests, apps suggestions, tips and cloud-based content (including Spotlight dynamic
backgrounds and News and Interests) will be turned off by setting Group Policies accordingly and by disabling some
related scheduled tasks.

Be aware that some of these features will be disabled only for the currently logged in user and for new users
created after running this procedure.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new SuggestionsDisabler(ui);
    }

    class NewGitHubIssueEntry : MenuEntry
    {
        public override string FullName => "Report an issue/Suggest a feature";
        public override string GetExplanation()
        {
            return
@"You will now be brought to a web page where you can open a GitHub issue in order to report a bug or to suggest
a new feature.";
        }
        public override IOperation CreateNewOperation(IUserInterface ui)
            => new BrowserOpener("https://github.com/Fs00/Win10BloatRemover/issues/new");
    }

    class AboutEntry : MenuEntry
    {
        public override string FullName => "About this program";
        public override string GetExplanation()
        {
            Version programVersion = GetType().Assembly.GetName().Version!;
            return
$@"Windows 10 Bloat Remover and Tweaker {programVersion.Major}.{programVersion.Minor} for Windows version {Program.MINIMUM_SUPPORTED_WINDOWS_VERSION} or higher
Developed by Fs00
Official GitHub repository: github.com/Fs00/Win10BloatRemover

Originally based on Windows 10 de-botnet guide by Federico Dossena: http://fdossena.com
Credits to all open source projects whose work has been used to improve this software:
  - privacy.sexy website: github.com/undergroundwires/privacy.sexy
  - Debloat Windows 10 scripts: github.com/W4RH4WK/Debloat-Windows-10

This software is released under BSD 3-Clause Clear license (continue to read full text).";
        }
        public override IOperation CreateNewOperation(IUserInterface ui) => new LicensePrinter(ui);
    }

    class QuitEntry : MenuEntry
    {
        private readonly RebootRecommendedFlag rebootFlag;

        public QuitEntry(RebootRecommendedFlag rebootFlag) => this.rebootFlag = rebootFlag;

        public override string FullName => "Exit the application";
        public override bool ShouldQuit => true;
        public override string GetExplanation() => "Are you sure?";
        public override IOperation CreateNewOperation(IUserInterface ui) => new AskForRebootOperation(ui, rebootFlag);
    }
}
