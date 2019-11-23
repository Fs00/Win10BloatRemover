using Microsoft.Win32;
using System;
using System.Diagnostics;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    class OneDriveRemover : IOperation
    {
        private enum UninstallationOutcome
        {
            Successful,
            Failed
        }

        public void PerformTask()
        {
            DisableOneDriveViaRegistryEdits();

            SystemUtils.KillProcess("onedrive");
            var uninstallationOutcome = RunOneDriveUninstaller();
            if (uninstallationOutcome == UninstallationOutcome.Failed)
                ThrowIfUserWantsToAbort();

            RemoveOneDriveLeftovers();

            Console.WriteLine();
            InstallWimTweak.RemoveComponentIfAllowed("Microsoft-Windows-OneDrive-Setup");
        }

        private void DisableOneDriveViaRegistryEdits()
        {
            ConsoleUtils.WriteLine("Disabling OneDrive via registry edits...", ConsoleColor.Green);
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using (RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
            using (RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Microsoft\OneDrive"))
                key.SetValue("PreventNetworkTrafficPreUserSignIn", 1, RegistryValueKind.DWord);
        }

        private void ThrowIfUserWantsToAbort()
        {
            ConsoleUtils.Write(
                "Uninstallation failed due to an unknown error. Do you still want to continue the process by " +
                "removing all leftover OneDrive files (including its application files for the current user) " +
                "and registry keys? (y/N) ",
                ConsoleColor.DarkYellow
            );
            if (Console.ReadKey().Key != ConsoleKey.Y)
                throw new Exception("The user aborted the operation.");
        }

        private UninstallationOutcome RunOneDriveUninstaller()
        {
            ConsoleUtils.WriteLine("\nExecuting OneDrive uninstaller...", ConsoleColor.Green);
            string installerPath = RetrieveOneDriveInstallerPath();
            int exitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(installerPath, "/uninstall");
            if (exitCode == 0)
                Console.WriteLine("Uninstallation completed succesfully.");
            return exitCode == 0 ? UninstallationOutcome.Successful : UninstallationOutcome.Failed;
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
            ConsoleUtils.WriteLine("\nRemoving OneDrive leftovers...", ConsoleColor.Green);
            SystemUtils.KillProcess("explorer");
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            Process.Start("explorer");
        }

        private void RemoveResidualFiles()
        {
            Console.WriteLine("Removing old files...");
            SystemUtils.TryDeleteDirectoryIfExists(@"C:\OneDriveTemp");
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\OneDrive");
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive");
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive");
            SystemUtils.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive");
        }

        private void RemoveResidualRegistryKeys()
        {
            Console.WriteLine("Deleting old registry keys...");
            using RegistryKey classesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            using RegistryKey key = classesRoot.OpenSubKey(@"CLSID", writable: true);
            key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
        }
    }
}
