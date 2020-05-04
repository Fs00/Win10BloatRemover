using System;
using Win10BloatRemover.Operations;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover
{
    abstract class MenuEntry
    {
        #nullable disable warnings
        protected IUserInterface ui;
        #nullable restore warnings

        public abstract string FullName { get; }
        public virtual bool ShouldQuit => false;
        public abstract string GetExplanation();
        public virtual IOperation? CreateNewOperation() => null;
    }

    class SystemAppsRemovalEnablingEntry : MenuEntry
    {
        public SystemAppsRemovalEnablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Make system apps removable";
        public override string GetExplanation()
        {
            return "This procedure will edit an internal database to allow the removal of system UWP apps " +
                   "such as Edge, Security Center, Connect via normal PowerShell methods.\n" +
                   "A backup of the database will be saved in the current directory. " +
                   "It will become useless as soon as you install/remove an app, so make sure you don't have " +
                   "any problems with Windows Update or the Store after the operation is completed.\n\n" +
                   "REMOVING SYSTEM APPS MAY POSSIBLY BREAK SOME FUNCTIONALITY; PROCEED AT YOUR OWN RISK.\n" +
                   "Remember also that certain apps are reinstalled after any Windows cumulative update.\n" +
                   "Before starting, make sure that the Store is not installing/updating apps in the background.";
        }
        public override IOperation CreateNewOperation() => new SystemAppsRemovalEnabler(ui);
    }

    class UWPAppRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;
        private readonly InstallWimTweak installWimTweak;

        public UWPAppRemovalEntry(IUserInterface ui, Configuration configuration, InstallWimTweak installWimTweak)
        {
            this.ui = ui;
            this.configuration = configuration;
            this.installWimTweak = installWimTweak;
        }

        public override string FullName => "Remove UWP apps";
        public override string GetExplanation()
        {
            string explanation = "The following groups of UWP apps will be removed:\n";
            foreach (UWPAppGroup app in configuration.UWPAppsToRemove)
                explanation += $"  {app}\n";
            explanation += "Some specific app-related services will also be removed " +
                           "(but backed up in case you need to restore them).\n" +
                           "In order to remove Edge, Connect and some components of Xbox, you need to make system apps removable first.";
            
            if (configuration.UWPAppsRemovalMode == UWPAppRemovalMode.RemoveProvisionedPackages)
                explanation += "\n\nAs specified in configuration file, provisioned packages of the " +
                               "aforementioned apps will be removed too (if available).\n" +
                               "This means that those apps won't be installed to new users when they log in for the first time.\n" +
                               @"To prevent this behaviour, change UWPAppsRemovalMode option to ""KeepProvisionedPackages"".";
            return explanation;
        }
        public override IOperation CreateNewOperation()
            => new UWPAppRemover(configuration.UWPAppsToRemove, configuration.UWPAppsRemovalMode, ui, installWimTweak);
    }

    class WinDefenderRemovalEntry : MenuEntry
    {
        private readonly InstallWimTweak installWimTweak;

        public WinDefenderRemovalEntry(IUserInterface ui, InstallWimTweak installWimTweak)
        {
            this.ui = ui;
            this.installWimTweak = installWimTweak;
        }

        public override string FullName => "Remove Windows Defender";
        public override string GetExplanation()
        {
            return "Important: Before starting, disable Tamper protection in Windows Security " +
                   "under Virus & threat protection settings.\n" +
                   "Defender will be removed using install-wim-tweak and disabled via Group Policies.\n\n" +
                   "If you have already made system apps removable, Security Center app will be removed too; " +
                   "otherwise, its menu icon will remain there, but the app won't start anymore.\n" +
                   "Remember that any Windows cumulative update is likely to reinstall the app.";
        }

        public override IOperation CreateNewOperation()
        {
            return new WindowsDefenderRemover(
                ui, installWimTweak,
                new UWPAppRemover(
                    new[] { UWPAppGroup.SecurityCenter },
                    UWPAppRemovalMode.KeepProvisionedPackages,
                    ui, installWimTweak
                )
            );
        }
    }

    class EdgeRemovalEntry : MenuEntry
    {
        public EdgeRemovalEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Remove Microsoft Edge";
        public override string GetExplanation()
        {
            return "You need to make system apps removable first, otherwise the uninstallation will fail.\n" +
                   @"You can also perform this task using UWP apps removal (""Edge"" must be included in the list " +
                   "\"UWPAppsToRemove\" in configuration file).\n" + 
                   "Take note that this app will likely be reinstalled after any Windows cumulative update. Proceed " +
                   "only if you know the consequences and risks of uninstalling system apps.";
        }
        public override IOperation CreateNewOperation()
            => new UWPAppRemover(new[] { UWPAppGroup.Edge }, UWPAppRemovalMode.KeepProvisionedPackages, ui, installWimTweak: null!);
    }

    class OneDriveRemovalEntry : MenuEntry
    {
        public OneDriveRemovalEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Remove OneDrive";
        public override string GetExplanation()
        {
            return "OneDrive will be first disabled using Group Policies, and then uninstalled using its setup program.\n" +
                   "Futhermore, its setup will be prevented from running when an user logs in for the first time.";
        }
        public override IOperation CreateNewOperation() => new OneDriveRemover(ui);
    }

    class ServicesRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public ServicesRemovalEntry(IUserInterface ui, Configuration configuration)
        {
            this.ui = ui;
            this.configuration = configuration;
        }

        public override string FullName => "Remove miscellaneous services";
        public override string GetExplanation()
        {
            string explanation = "The services starting with the following names will be removed:\n";
            foreach (string service in configuration.ServicesToRemove)
                explanation += $"  {service}\n";
            return explanation + "Services will be backed up in the same folder as this program executable.";
        }
        public override IOperation CreateNewOperation() => new ServiceRemover(configuration.ServicesToRemove, ui);
    }

    class WindowsFeaturesRemovalEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public WindowsFeaturesRemovalEntry(IUserInterface ui, Configuration configuration)
        {
            this.ui = ui;
            this.configuration = configuration;
        }

        public override string FullName => "Remove Windows features";
        public override string GetExplanation()
        {
            string explanation = "The following features will be removed:";
            foreach (string feature in configuration.WindowsFeaturesToRemove)
                explanation += $"\n  {feature}";
            return explanation;
        }
        public override IOperation CreateNewOperation() => new FeaturesRemover(configuration.WindowsFeaturesToRemove, ui);
    }

    class TelemetryDisablingEntry : MenuEntry
    {
        public TelemetryDisablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Disable telemetry";
        public override string GetExplanation()
        {
            return "This will backup and remove several telemetry-related services and disable features that " +
                   "report data to Microsoft, including MS Compatibility Telemetry, Device Census, " +
                   "SmartScreen, Steps Recorder and Compatibility Assistant.";
        }
        public override IOperation CreateNewOperation() => new TelemetryDisabler(ui);
    }

    class CortanaDisablingEntry : MenuEntry
    {
        public CortanaDisablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Disable Cortana and web search";
        public override string GetExplanation()
        {
            return "By editing Group Policies, Cortana will be disabled and Windows Search won't display results " +
                   "from the web anymore.\n" +
                   "A firewall rule will also be added to prevent Cortana from connecting to the Internet.";
        }
        public override IOperation CreateNewOperation() => new CortanaWebSearchDisabler(ui);
    }

    class AutoUpdatesDisablingEntry : MenuEntry
    {
        public AutoUpdatesDisablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Disable automatic updates";
        public override string GetExplanation()
        {
            return "Windows and Store apps automatic updates will be disabled using Group Policies.\n" + 
                   "This method won't work on Windows 10 Home.";
        }
        public override IOperation CreateNewOperation() => new AutoUpdatesDisabler(ui);
    }

    class ScheduledTasksDisablingEntry : MenuEntry
    {
        private readonly Configuration configuration;

        public ScheduledTasksDisablingEntry(IUserInterface ui, Configuration configuration)
        {
            this.ui = ui;
            this.configuration = configuration;
        }

        public override string FullName => "Disable miscellaneous scheduled tasks";
        public override string GetExplanation()
        {
            string explanation = "The following scheduled tasks will be disabled:";
            foreach (string task in configuration.ScheduledTasksToDisable)
                explanation += $"\n  {task}";
            return explanation;
        }
        public override IOperation CreateNewOperation()
            => new ScheduledTasksDisabler(configuration.ScheduledTasksToDisable, ui);
    }

    class ErrorReportingDisablingEntry : MenuEntry
    {
        public ErrorReportingDisablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Disable Windows Error Reporting";
        public override string GetExplanation()
        {
            return "Windows Error Reporting will disabled by editing Group Policies, as well as by removing " +
                   "its services (after backing them up).";
        }
        public override IOperation CreateNewOperation() => new ErrorReportingDisabler(ui);
    }

    class TipsAndFeedbackDisablingEntry : MenuEntry
    {
        public TipsAndFeedbackDisablingEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "Disable tips and feedback requests";
        public override string GetExplanation()
        {
            return "Feedback notifications/requests, apps suggestions, tips and Spotlight (including dynamic lock " +
                   "screen backgrounds) will be turned off by setting Group Policies accordingly and by disabling " +
                   "some related scheduled tasks.";
        }
        public override IOperation CreateNewOperation() => new TipsDisabler(ui);
    }

    class NewGitHubIssueEntry : MenuEntry
    {
        public override string FullName => "Report an issue/Suggest a feature";
        public override string GetExplanation()
        {
            return "Your browser will now open on a GitHub page where you will be able to " +
                   "open an issue to report a bug or suggest a new feature.";
        }
        public override IOperation CreateNewOperation()
            => new BrowserOpener("https://github.com/Fs00/Win10BloatRemover/issues/new");
    }

    class AboutEntry : MenuEntry
    {
        public AboutEntry(IUserInterface ui) => this.ui = ui;

        public override string FullName => "About this program";
        public override string GetExplanation()
        {
            Version programVersion = GetType().Assembly.GetName().Version!;
            return $"Windows 10 Bloat Remover and Tweaker {programVersion.Major}.{programVersion.Minor} " +
                   $"for Windows version {programVersion.Build}\n\n" +
                   "Developed by Fs00\n" +
                   "Official GitHub repository: github.com/Fs00/Win10BloatRemover\n" +
                   "Based on Windows 10 de-botnet guide by Federico Dossena: fdossena.com\n\n" +
                   "This software is released under BSD 3-Clause Clear license (continue to read full text).";
        }
        public override IOperation CreateNewOperation() => new LicensePrinter(ui);
    }

    class QuitEntry : MenuEntry
    {
        public override string FullName => "Exit the application";
        public override bool ShouldQuit => true;
        public override string GetExplanation() => "Are you sure?";
    }
}
