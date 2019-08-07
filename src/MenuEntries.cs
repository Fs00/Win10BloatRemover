using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    abstract class MenuEntry
    {
        public abstract string FullName { get; }
        public virtual string GetExplanation() => string.Empty;
        public virtual IOperation GetOperationInstance() => null;
    }

    class SystemAppRemovalEnablingEntry : MenuEntry
    {
        public override string FullName => "Make system apps removable";
        public override string GetExplanation()
        {
            return "This procedure will edit an internal database to allow the removal of system UWP apps " +
                   "such as Edge, Security Center, Connect via normal PowerShell methods.\n" +
                   "A backup of the database will be saved in the current directory. " +
                   "It will become useless as soon as you install/remove an app, so make sure you don't have " +
                   "any problems with Windows Update or the Store after the operation is completed.\n" +
                   "Take note that you might not receive any more Windows feature updates after applying these modifications.\n" +
                   @"If you get the error ""attempt to write a readonly database"", try again and make sure that " +
                   "the Store is not installing/updating apps in the background.";
        }
        public override IOperation GetOperationInstance() => new SystemAppRemovalEnabler();
    }

    class UWPAppRemovalEntry : MenuEntry
    {
        public override string FullName => "UWP apps removal";
        public override string GetExplanation()
        {
            string explanation = "The following groups of UWP apps will be removed:\n";
            foreach (UWPAppGroup app in Configuration.Instance.UWPAppsToRemove)
                explanation += $"  {app.ToString()}\n";
            explanation += "Some specific app-related services will also be removed " +
                           "(but backed up in case you need to restore them).\n" +
                           "In order to remove Edge, Connect and some components of Xbox, you need to make system apps removable first.";
            
            if (Configuration.Instance.UWPAppsRemovalMode == UWPAppRemovalMode.RemoveProvisionedPackages)
                explanation += "\n\nAs specified in configuration file, provisioned packages of the " +
                               "aforementioned apps will be removed too (if available).\n" +
                               "This means that those apps won't be installed to new users when they log in for the first time.\n" +
                               @"To prevent this behaviour, change UWPAppsRemovalMode option to ""KeepProvisionedPackages"".";
            return explanation;
        }

        public override IOperation GetOperationInstance()
            => new UWPAppRemover(Configuration.Instance.UWPAppsToRemove, Configuration.Instance.UWPAppsRemovalMode);
    }

    class WinDefenderRemovalEntry : MenuEntry
    {
        public override string FullName => "Windows Defender removal";
        public override string GetExplanation()
        {
            return "Important: Before starting, disable Tamper protection in Windows Security " +
                   "under Virus & threat protection settings.\n" +
                   "Defender will be removed using install-wim-tweak and disabled via Group Policies.\n" +
                   "If you have already made system apps removable, Security Center app will be removed too; " +
                   "otherwise, its menu icon will remain there, but the app won't start anymore.";
        }
        public override IOperation GetOperationInstance() => new WindowsDefenderRemover();
    }

    class EdgeRemovalEntry : MenuEntry
    {
        public override string FullName => "Microsoft Edge removal";
        public override string GetExplanation()
        {
            return "Starting from Windows 10 version 1903, install-wim-tweak can no longer be used to remove " +
                   "Edge since it breaks the installation of cumulative updates.\n" +
                   @"To accomplish this task, add ""Edge"" to the list ""UWPAppsToRemove"" in configuration " +
                   "file, make system apps removable and then use UWP apps removal.";
        }
    }

    class OneDriveRemovalEntry : MenuEntry
    {
        public override string FullName => "OneDrive removal";
        public override string GetExplanation()
        {
            return "If you allow the use of install-wim-tweak, this will prevent the app to be installed for " +
                   "new users and to return after a reset or a major Windows update.";
        }
        public override IOperation GetOperationInstance() => new OneDriveRemover();
    }

    class ServicesRemovalEntry : MenuEntry
    {
        public override string FullName => "Miscellaneous services removal";
        public override string GetExplanation()
        {
            string explanation = "The services starting with the following names will be removed:\n";
            foreach (string service in Configuration.Instance.ServicesToRemove)
                explanation += $"  {service}\n";
            return explanation + "Services will be backed up in the same folder as this program executable.";
        }
        public override IOperation GetOperationInstance() => new ServiceRemover(Configuration.Instance.ServicesToRemove);
    }

    class WindowsFeaturesRemovalEntry : MenuEntry
    {
        public override string FullName => "Windows features removal";
        public override string GetExplanation()
        {
            string explanation = "The following features will be removed:\n";
            foreach (string feature in Configuration.Instance.WindowsFeaturesToRemove)
                explanation += $"  {feature}\n";
            return explanation;
        }
        public override IOperation GetOperationInstance() => new FeaturesRemover(Configuration.Instance.WindowsFeaturesToRemove);
    }

    class TelemetryDisablingEntry : MenuEntry
    {
        public override string FullName => "Telemetry disabling";
        public override string GetExplanation()
        {
            return "This will backup and remove several telemetry-related services and disable features that " +
                   "report data to Microsoft, including MS Compatibility Telemetry, Device Census, " +
                   "SmartScreen, Steps Recorder and Compatibility Assistant.";
        }
        public override IOperation GetOperationInstance() => new TelemetryDisabler();
    }

    class CortanaDisablingEntry : MenuEntry
    {
        public override string FullName => "Cortana disabling";
        public override string GetExplanation()
        {
            return "This won't remove Cortana (otherwise the system would break), it will only be disabled " +
                   "using Group Policy and blocked by the firewall.";
        }
        public override IOperation GetOperationInstance() => new CortanaDisabler();
    }

    class AutoUpdatesDisablingEntry : MenuEntry
    {
        public override string FullName => "Automatic Windows updates disabling";
        public override string GetExplanation()
        {
            return "Windows and Store apps automatic updates will be disabled using Group Policies.\n" + 
                   "This method won't work on Windows 10 Home.";
        }
        public override IOperation GetOperationInstance() => new AutoUpdatesDisabler();
    }

    class ScheduledTasksDisablingEntry : MenuEntry
    {
        public override string FullName => "Miscellaneous scheduled tasks disabling";
        public override string GetExplanation()
        {
            string explanation = "The following scheduled tasks will be disabled:\n";
            foreach (string task in Configuration.Instance.ScheduledTasksToDisable)
                explanation += $"  {task}\n";
            return explanation;
        }

        public override IOperation GetOperationInstance()
            => new ScheduledTasksDisabler(Configuration.Instance.ScheduledTasksToDisable);
    }

    class ErrorReportingDisablingEntry : MenuEntry
    {
        public override string FullName => "Windows Error Reporting disabling";
        public override string GetExplanation()
        {
            return "Windows Error Reporting will disabled by editing Group Policies, as well as by removing " +
                   "its services (after backing them up).";
        }
        public override IOperation GetOperationInstance() => new ErrorReportingDisabler();
    }

    class TipsAndFeedbackDisablingEntry : MenuEntry
    {
        public override string FullName => "Windows Tips and feedback requests disabling";
        public override IOperation GetOperationInstance() => new WindowsTipsDisabler();
    }

    class NewGitHubIssueEntry : MenuEntry
    {
        public override string FullName => "Report an issue/Suggest a feature";
        public override string GetExplanation()
        {
            return "Your browser will now open on a GitHub page where you will be able to " +
                   "open an issue to report a bug or suggest a new feature.";
        }

        public override IOperation GetOperationInstance()
            => new BrowserOpener("https://github.com/Fs00/Win10BloatRemover/issues/new");
    }

    class CreditsEntry : MenuEntry
    {
        public override string FullName => "Credits and license";
        public override string GetExplanation()
        {
            return "Developed by Fs00\n" +
                   "Official GitHub repository: github.com/Fs00/Win10BloatRemover\n" +
                   "Based on Windows 10 de-botnet guide by Federico Dossena: fdossena.com\n\n" +
                   "This software is released under BSD 3-Clause Clear license (continue to read full text).";
        }
        public override IOperation GetOperationInstance() => new LicensePrinter();
    }

    class QuitEntry : MenuEntry
    {
        public override string FullName => "Exit the application";
        public override string GetExplanation() => "Are you sure?";
    }
}
