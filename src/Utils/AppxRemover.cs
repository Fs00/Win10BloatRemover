using Microsoft.Win32;
using System;
using System.Linq;
using System.Management.Automation;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover.Utils
{
    public class AppxRemover
    {
        public struct Result
        {
            public int RemovedApps { private set; get; }
            public int FailedRemovals { private set; get; }

            public Result(int removedApps, int failedRemovals)
            {
                RemovedApps = removedApps;
                FailedRemovals = failedRemovals;
            }
        }

        private enum RemovalOutcome
        {
            NotInstalled,
            Success,
            Failure
        }

        private readonly IUserInterface ui;

        private /*lateinit*/ PowerShell powerShell = default!;

        public AppxRemover(IUserInterface ui) => this.ui = ui;

        public Result RemoveAppsForCurrentUser(params string[] appNames)
        {
            using (powerShell = PowerShellExtensions.CreateWithImportedModules("AppX").WithOutput(ui))
            {
                var removalMethod = new CurrentUserRemovalMethod(ui, powerShell);
                return PerformAppsRemoval(appNames, removalMethod);
            }
        }

        public Result RemoveAppsForAllUsers(params string[] appNames)
        {
            using (powerShell = PowerShellExtensions.CreateWithImportedModules("AppX", "Dism").WithOutput(ui))
            {
                var removalMethod = new AllUsersRemovalMethod(ui, powerShell);
                return PerformAppsRemoval(appNames, removalMethod);
            }
        }

        private Result PerformAppsRemoval(string[] appNames, RemovalMethod removalMethod)
        {
            int removedApps = 0, failedRemovals = 0;
            foreach (string appName in appNames)
            {
                RemovalOutcome outcome = removalMethod.RemovePackagesForApp(appName);
                if (outcome == RemovalOutcome.Success)
                    removedApps++;
                else if (outcome == RemovalOutcome.Failure)
                    failedRemovals++;
            }
            return new Result(removedApps, failedRemovals);
        }

        private abstract class RemovalMethod
        {
            protected readonly IUserInterface ui;

            protected RemovalMethod(IUserInterface ui) => this.ui = ui;

            public virtual RemovalOutcome RemovePackagesForApp(string appName)
            {
                // Most apps are made up by a single package, but it's not always the case
                // (e.g. some apps might have both x86 and x64 variants installed)
                var appPackages = GetAppPackages(appName);
                if (appPackages.Length == 0)
                {
                    ui.PrintMessage($"App {appName} is not installed.");
                    return RemovalOutcome.NotInstalled;
                }

                ui.PrintMessage($"Uninstalling app {appName}...");
                foreach (var package in appPackages)
                {
                    var outcome = RemoveAppPackage(package);
                    if (outcome == RemovalOutcome.Failure)
                        return outcome;
                }
                return RemovalOutcome.Success;
            }

            protected bool IsSystemApp(dynamic package)
            {
                string systemAppsFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\SystemApps";
                return package.InstallLocation.StartsWith(systemAppsFolder);
            }

            protected abstract dynamic[] GetAppPackages(string appName);
            protected abstract RemovalOutcome RemoveAppPackage(dynamic package);
        }

        private class CurrentUserRemovalMethod : RemovalMethod
        {
            private readonly PowerShell powerShell;

            public CurrentUserRemovalMethod(IUserInterface ui, PowerShell powerShell) : base(ui)
                => this.powerShell = powerShell;

            protected override dynamic[] GetAppPackages(string appName)
                => powerShell.Run($"Get-AppxPackage -Name {appName}");

            protected override RemovalOutcome RemoveAppPackage(dynamic package)
            {
                if (IsSystemApp(package))
                {
                    // Even though removing a system app for a single user is technically possible, we disallow that
                    // since cumulative updates would reinstall the app anyway, unless we prevent its reinstallation for all users
                    ui.PrintNotice("Uninstallation skipped. This is a system app, and therefore can only be removed for all users.");
                    return RemovalOutcome.Failure;
                }

                powerShell.Run($"Remove-AppxPackage -Package {package.PackageFullName}");
                return powerShell.Streams.Error.Count == 0 ? RemovalOutcome.Success : RemovalOutcome.Failure;
            }
        }

        private class AllUsersRemovalMethod : RemovalMethod
        {
            private readonly PowerShell powerShell;

            public AllUsersRemovalMethod(IUserInterface ui, PowerShell powerShell) : base(ui)
                => this.powerShell = powerShell;

            public override RemovalOutcome RemovePackagesForApp(string appName)
            {
                // Starting from version 2004, uninstalling an app for all users raises an error
                // if the provisioned package has not been already removed.
                var outcome = RemoveAppProvisionedPackage(appName);
                if (outcome == RemovalOutcome.Failure)
                    return outcome;

                return base.RemovePackagesForApp(appName);
            }

            private RemovalOutcome RemoveAppProvisionedPackage(string appName)
            {
                var provisionedPackage = powerShell.Run("Get-AppxProvisionedPackage -Online")
                    .FirstOrDefault(package => package.DisplayName == appName);
                if (provisionedPackage == null)
                    return RemovalOutcome.NotInstalled;

                ui.PrintMessage($"Removing provisioned package for app {appName}...");
                powerShell.Run(
                    $"Remove-AppxProvisionedPackage -Online -PackageName \"{provisionedPackage.PackageName}\""
                );
                return powerShell.Streams.Error.Count == 0 ? RemovalOutcome.Success : RemovalOutcome.Failure;
            }

            protected override dynamic[] GetAppPackages(string appName)
                => powerShell.Run($"Get-AppxPackage -AllUsers -Name {appName}");

            protected override RemovalOutcome RemoveAppPackage(dynamic package)
            {
                if (IsSystemApp(package))
                    MakeSystemAppRemovable(package);

                powerShell.Run($"Remove-AppxPackage -AllUsers -Package {package.PackageFullName}");
                return powerShell.Streams.Error.Count == 0 ? RemovalOutcome.Success : RemovalOutcome.Failure;
            }

            private void MakeSystemAppRemovable(dynamic package)
            {
                using var appxStoreKey = RegistryUtils.LocalMachine64.OpenSubKeyWritable(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore"
                );
                AddEndOfLifeKeysForPackage(package.PackageFullName, appxStoreKey);

                // This prevents the app from being installed for new users and after cumulative updates
                RemovePackageFromInboxAppsRegistry(package.Name, appxStoreKey);
            }

            private void AddEndOfLifeKeysForPackage(string packageFullName, RegistryKey appxStoreKey)
            {
                var allUserSids = appxStoreKey.GetSubKeyNames().Where(keyName => keyName.StartsWith("S-1-5-"));
                foreach (string userSid in allUserSids)
                    appxStoreKey.CreateSubKey($@"EndOfLife\{userSid}\{packageFullName}");
            }

            private void RemovePackageFromInboxAppsRegistry(string packageName, RegistryKey appxStoreKey)
            {
                // The InboxApplications subkey has some special permissions that prevent it from being opened with write permissions.
                // Therefore we need to open it with read permissions and then use the parent key (which has been opened
                // with write permissions) to delete its subkeys.
                using var inboxAppsRegistry = appxStoreKey.OpenSubKey("InboxApplications")!;
                string? inboxAppKey = inboxAppsRegistry.GetSubKeyNames()
                    .FirstOrDefault(inboxAppName => inboxAppName.StartsWith($"{packageName}_"));

                if (inboxAppKey != null)
                    appxStoreKey.DeleteSubKeyTree($@"InboxApplications\{inboxAppKey}");
            }
        }
    }
}
