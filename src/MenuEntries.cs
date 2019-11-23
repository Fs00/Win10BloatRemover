using System;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    abstract class MenuEntry
    {
        public abstract string FullName { get; }
        public abstract string GetExplanation();
        public virtual IOperation? GetOperationInstance() => null;
    }

    class SystemAppsRemovalEnablingEntry : MenuEntry
    {
        public override string FullName => "Make system apps removable";
        public override string GetExplanation()
        {
            return "This procedure will edit an internal database to allow the removal of system UWP apps " +
                   "such as Edge, Security Center, Connect via normal PowerShell methods.\n" +
                   "A backup of the database will be saved in the current directory. " +
                   "It will become useless as soon as you install/remove an app, so make sure you don't have " +
                   "any problems with Windows Update or the Store after the operation is completed.\n\n" +
                   "REMOVING SYSTEM APPS CAN PREVENT WINDOWS UPDATES FROM BEING INSTALLED; PROCEED AT YOUR OWN RISK.\n" +
                   "Remember also that certain apps are reinstalled after any Windows cumulative update.\n" +
                   "Before starting, make sure that the Store is not installing/updating apps in the background.";
        }
        public override IOperation GetOperationInstance() => new SystemAppsRemovalEnabler();
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
                   "Defender will be removed using install-wim-tweak and disabled via Group Policies.\n\n" +
                   "If you have already made system apps removable, Security Center app will be removed too; " +
                   "otherwise, its menu icon will remain there, but the app won't start anymore.\n" +
                   "Remember that any Windows cumulative update is likely to reinstall the app.";
        }
        public override IOperation GetOperationInstance() => new WindowsDefenderRemover();
    }

    class EdgeRemovalEntry : MenuEntry
    {
        public override string FullName => "Microsoft Edge removal";
        public override string GetExplanation()
        {
            return "You need to make system apps removable first, otherwise the uninstallation will fail.\n" +
                   @"You can also perform this task using UWP apps removal (""Edge"" must be included in the list " +
                   "\"UWPAppsToRemove\" in configuration file).\n" + 
                   "Take note that this app will likely be reinstalled after any Windows cumulative update. Proceed " +
                   "ONLY if you know the consequences and risks of uninstalling system apps.";
        }
        public override IOperation GetOperationInstance()
            => new UWPAppRemover(new[] { UWPAppGroup.Edge }, UWPAppRemovalMode.KeepProvisionedPackages);
    }

    class OneDriveRemovalEntry : MenuEntry
    {
        public override string FullName => "OneDrive removal";
        public override string GetExplanation()
        {
            return "OneDrive will be first disabled using Group Policies, and then uninstalled using its setup program.\n" +
                   "If you allow the use of install-wim-tweak, the setup program will also be removed from the " +
                   "system so that the app won't be installed for new users.";
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
            string explanation = "The following features will be removed:";
            foreach (string feature in Configuration.Instance.WindowsFeaturesToRemove)
                explanation += $"\n  {feature}";
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
            string explanation = "The following scheduled tasks will be disabled:";
            foreach (string task in Configuration.Instance.ScheduledTasksToDisable)
                explanation += $"\n  {task}";
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
        public override string GetExplanation()
        {
            return "Feedback notifications/requests, apps suggestions, tips and Spotlight (including dynamic lock " +
                   "screen backgrounds) will be turned off by setting Group Policies accordingly and by disabling " +
                   "some related scheduled tasks.";
        }
        public override IOperation GetOperationInstance() => new TipsDisabler();
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

    class AboutEntry : MenuEntry
    {
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
        public override IOperation GetOperationInstance() => new LicensePrinter();
    }

    class QuitEntry : MenuEntry
    {
        public override string FullName => "Exit the application";
        public override string GetExplanation() => "Are you sure?";
    }
}
