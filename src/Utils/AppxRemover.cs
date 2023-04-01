using Microsoft.Win32;
using System;
using System.Linq;
using Win10BloatRemover.UI;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Win10BloatRemover.Utils
{
    public class AppxRemover
    {
        public readonly record struct Result(int RemovedApps, int FailedRemovals);

        private enum RemovalOutcome
        {
            NotInstalled,
            Success,
            Failure
        }

        private readonly IUserInterface ui;

        public AppxRemover(IUserInterface ui) => this.ui = ui;

        public Result RemoveAppsForCurrentUser(params string[] appNames)
        {
            return PerformAppsRemoval(appNames, new CurrentUserRemovalMethod(ui));
        }

        public Result RemoveAppsForAllUsers(params string[] appNames)
        {
            return PerformAppsRemoval(appNames, new AllUsersRemovalMethod(ui));
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
            protected readonly PackageManager packageManager = new PackageManager();

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
                    var outcome = TryRemoveAppPackage(package);
                    if (outcome == RemovalOutcome.Failure)
                        return outcome;
                }
                return RemovalOutcome.Success;
            }

            private RemovalOutcome TryRemoveAppPackage(Package package)
            {
                try
                {
                    return RemoveAppPackage(package);
                }
                catch (Exception exc)
                {
                    PrintUninstallationError(exc);
                    return RemovalOutcome.Failure;
                }
            }

            protected void PrintUninstallationError(Exception exc)
            {
                string errorMessage = "Uninstallation failed: ";
                if (exc.InnerException != null)
                    errorMessage += exc.InnerException.Message;
                else
                    errorMessage += exc.Message;

                ui.PrintError(errorMessage);
            }

            protected bool IsSystemApp(Package package)
            {
                string systemAppsFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\SystemApps";
                return package.InstalledPath.StartsWith(systemAppsFolder);
            }

            protected abstract Package[] GetAppPackages(string appName);
            protected abstract RemovalOutcome RemoveAppPackage(Package package);
        }

        private class CurrentUserRemovalMethod : RemovalMethod
        {
            public CurrentUserRemovalMethod(IUserInterface ui) : base(ui) {}

            protected override Package[] GetAppPackages(string appName)
            {
                const string currentUser = "";
                return packageManager.FindPackagesForUser(currentUser)
                        .Where(package => package.Id.Name == appName)
                        .ToArray();
            }

            protected override RemovalOutcome RemoveAppPackage(Package package)
            {
                if (IsSystemApp(package))
                {
                    // Even though removing a system app for a single user is technically possible, we disallow that
                    // since cumulative updates would reinstall the app anyway, unless we prevent its reinstallation for all users
                    ui.PrintNotice("Uninstallation skipped. This is a system app, and therefore can only be removed for all users.");
                    return RemovalOutcome.Failure;
                }

                packageManager.RemovePackageAsync(package.Id.FullName).AsTask().Wait();
                return RemovalOutcome.Success;
            }
        }

        private class AllUsersRemovalMethod : RemovalMethod
        {
            public AllUsersRemovalMethod(IUserInterface ui) : base(ui) {}

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
                var provisionedPackage = packageManager.FindProvisionedPackages()
                    .FirstOrDefault(package => package.Id.Name == appName);
                if (provisionedPackage == null)
                    return RemovalOutcome.NotInstalled;

                ui.PrintMessage($"Removing provisioned package for app {appName}...");
                try
                {
                    packageManager.DeprovisionPackageForAllUsersAsync(provisionedPackage.Id.FamilyName).AsTask().Wait();
                    return RemovalOutcome.Success;
                }
                catch (Exception exc)
                {
                    PrintUninstallationError(exc);
                    return RemovalOutcome.Failure;
                }
            }

            protected override Package[] GetAppPackages(string appName)
            {
                return packageManager.FindPackages()
                        .Where(package => package.Id.Name == appName)
                        .ToArray();
            }

            protected override RemovalOutcome RemoveAppPackage(Package package)
            {
                if (IsSystemApp(package))
                    MakeSystemAppRemovable(package);

                packageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.RemoveForAllUsers).AsTask().Wait();
                return RemovalOutcome.Success;
            }

            private void MakeSystemAppRemovable(Package package)
            {
                using var appxStoreKey = RegistryUtils.LocalMachine64.OpenSubKeyWritable(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore"
                );
                AddEndOfLifeKeysForPackage(package.Id.FullName, appxStoreKey);

                // This prevents the app from being installed for new users and after cumulative updates
                RemovePackageFromInboxAppsRegistry(package.Id.Name, appxStoreKey);
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
