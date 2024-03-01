using Microsoft.Win32;
using System.Threading;
using Win10BloatRemover.UI;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace Win10BloatRemover.Utils;

public class AppxRemover(IUserInterface ui)
{
    public readonly record struct Result(int RemovedApps, int FailedRemovals);

    private enum RemovalOutcome
    {
        NotInstalled,
        Success,
        Failure
    }

    public Result RemoveAppsForCurrentUser(params string[] appNames)
    {
        return PerformAppsRemoval(appNames, new CurrentUserRemovalMethod(ui));
    }

    public Result RemoveAppsForAllUsers(params string[] appNames)
    {
        using var dismClient = new DismClient();
        return PerformAppsRemoval(appNames, new AllUsersRemovalMethod(ui, dismClient));
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

    private static bool IsSystemApp(Package package)
    {
        return package.SignatureKind == PackageSignatureKind.System;
    }

    private abstract class RemovalMethod(IUserInterface ui)
    {
        protected readonly IUserInterface ui = ui;
        protected readonly PackageManager packageManager = new PackageManager();

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

        protected RemovalOutcome HandleResult(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation)
        {
            using var completionEvent = new ManualResetEvent(false);
            operation.Completed = (_, _) => { completionEvent.Set(); };
            completionEvent.WaitOne();

            if (operation.Status == AsyncStatus.Completed)
                return RemovalOutcome.Success;
            else
            {
                PrintError(operation);
                return RemovalOutcome.Failure;
            }
        }

        private void PrintError(IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> operation)
        {
            DeploymentResult result = operation.GetResults();
            string errorMessage = operation.ErrorCode?.Message?.Trim() ?? "Unknown error occurred";
            if (!string.IsNullOrEmpty(result.ErrorText))
                errorMessage += $"\n{result.ErrorText}";

            ui.PrintError(errorMessage);
        }

        protected abstract Package[] GetAppPackages(string appName);
        protected abstract RemovalOutcome RemoveAppPackage(Package package);
    }

    private class CurrentUserRemovalMethod(IUserInterface ui) : RemovalMethod(ui)
    {
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

            return HandleResult(packageManager.RemovePackageAsync(package.Id.FullName));
        }
    }

    private class AllUsersRemovalMethod(IUserInterface ui, DismClient dismClient) : RemovalMethod(ui)
    {
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
            var provisionedPackage = dismClient.FindAppxProvisionedPackageByName(appName);
            if (provisionedPackage == null)
                return RemovalOutcome.NotInstalled;

            ui.PrintMessage($"Removing provisioned package for app {appName}...");
            try
            {
                dismClient.RemoveAppxProvisionedPackage(provisionedPackage.PackageName);
                return RemovalOutcome.Success;
            }
            catch (Exception exc)
            {
                ui.PrintError($"Could not remove provisioned package {provisionedPackage.PackageName}: {exc.Message} (0x{exc.HResult:X8})");
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

            return HandleResult(
                packageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.RemoveForAllUsers)
            );
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
