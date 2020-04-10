using Microsoft.Win32;
using System;
using System.Diagnostics;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    public class OneDriveRemover : IOperation
    {
        private readonly IUserInterface ui;
        private readonly InstallWimTweak installWimTweak;

        public OneDriveRemover(IUserInterface ui, InstallWimTweak installWimTweak)
        {
            this.ui = ui;
            this.installWimTweak = installWimTweak;
        }

        public void Run()
        {
            EditRegistryKeysToDisableOneDrive();

            var uninstallationExitCode = RunOneDriveUninstaller();
            if (uninstallationExitCode == SystemUtils.EXIT_CODE_SUCCESS)
                ui.PrintMessage("Uninstallation completed succesfully.");
            else
            {
                ui.PrintError("Uninstallation failed due to an unknown error.");
                ThrowIfUserWantsToAbort();
            }

            RemoveOneDriveLeftovers();

            ui.PrintEmptySpace();
            installWimTweak.RemoveComponentIfAllowed("Microsoft-Windows-OneDrive-Setup", ui);
        }

        private void EditRegistryKeysToDisableOneDrive()
        {
            ui.PrintHeading("Disabling OneDrive via registry edits...");
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using (RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
            using (RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Microsoft\OneDrive"))
                key.SetValue("PreventNetworkTrafficPreUserSignIn", 1, RegistryValueKind.DWord);
        }

        private void ThrowIfUserWantsToAbort()
        {
            var choice = ui.AskUserConsent(
                "Do you still want to continue the process by removing all leftover OneDrive files (including its " +
                "application files for the current user) and registry keys?"
            );
            if (choice == IUserInterface.UserChoice.No)
                throw new Exception("The user aborted the operation.");
        }

        private int RunOneDriveUninstaller()
        {
            ui.PrintHeading("\nExecuting OneDrive uninstaller...");
            SystemUtils.KillProcess("onedrive");
            string installerPath = RetrieveOneDriveInstallerPath();
            return SystemUtils.RunProcessBlockingWithOutput(installerPath, "/uninstall", ui);
        }

        private string RetrieveOneDriveInstallerPath()
        {
            if (Env.Is64BitOperatingSystem)
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe";
            else
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\System32\OneDriveSetup.exe";
        }

        private void RemoveOneDriveLeftovers()
        {
            ui.PrintHeading("\nRemoving OneDrive leftovers...");
            SystemUtils.KillProcess("explorer");
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            Process.Start("explorer");
        }

        private void RemoveResidualFiles()
        {
            ui.PrintMessage("Removing old files...");
            SystemUtils.TryDeleteDirectoryIfExists(@"C:\OneDriveTemp", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", ui);
        }

        private void RemoveResidualRegistryKeys()
        {
            ui.PrintMessage("Deleting old registry keys...");
            using RegistryKey classesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            using RegistryKey key = classesRoot.OpenSubKey(@"CLSID", writable: true);
            key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
        }
    }
}
