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

        public OneDriveRemover(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableOneDrive();
            SystemUtils.KillProcess("onedrive");
            RunOneDriveUninstaller();
            RemoveOneDriveLeftovers();
            DisableAutomaticSetupForNewUsers();
        }

        private void DisableOneDrive()
        {
            ui.PrintMessage("Disabling OneDrive via registry edits...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Microsoft\OneDrive");
            key.SetValue("PreventNetworkTrafficPreUserSignIn", 1);
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

        private void RunOneDriveUninstaller()
        {
            ui.PrintMessage("Executing OneDrive uninstaller...");
            string setupPath = RetrieveOneDriveSetupPath();
            var uninstallationExitCode = SystemUtils.RunProcessBlockingWithOutput(setupPath, "/uninstall", ui);
            if (uninstallationExitCode != SystemUtils.EXIT_CODE_SUCCESS)
            {
                ui.PrintError("Uninstallation failed due to an unknown error.");
                ThrowIfUserWantsToAbort();
            }
        }

        private string RetrieveOneDriveSetupPath()
        {
            if (Env.Is64BitOperatingSystem)
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe";
            else
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\System32\OneDriveSetup.exe";
        }

        private void RemoveOneDriveLeftovers()
        {
            ui.PrintMessage("Removing OneDrive leftovers...");
            SystemUtils.KillProcess("explorer");
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            Process.Start("explorer");
        }

        private void RemoveResidualFiles()
        {
            SystemUtils.TryDeleteDirectoryIfExists(@"C:\OneDriveTemp", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", ui);
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", ui);
        }

        private void RemoveResidualRegistryKeys()
        {
            using RegistryKey classesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            using RegistryKey key = classesRoot.OpenSubKey(@"CLSID", writable: true);
            key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
        }

        // Borrowed from github.com/W4RH4WK/Debloat-Windows-10/blob/master/scripts/remove-onedrive.ps1
        private void DisableAutomaticSetupForNewUsers()
        {
            ui.PrintMessage("Disabling automatic OneDrive setup for new users...");
            int loadExitCode = SystemUtils.RunProcessBlocking("reg", @"load ""HKEY_USERS\_Default"" ""C:\Users\Default\NTUSER.DAT""");
            if (loadExitCode != SystemUtils.EXIT_CODE_SUCCESS)
                throw new Exception("Unable to load Default user registry hive.");
            using (RegistryKey key = Registry.Users.CreateSubKey(@"_Default\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                key.DeleteValue("OneDriveSetup", throwOnMissingValue: false);
            SystemUtils.RunProcessBlocking("reg", @"unload ""HKEY_USERS\_Default""");
        }
    }
}
