using Microsoft.Win32;
using System;
using System.Diagnostics;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    class OneDriveRemover : IOperation
    {
        public void PerformTask()
        {
            KillProcess("onedrive.exe");
            RunOneDriveUninstaller();
            DisableOneDriveViaGroupPolicies();

            KillProcess("explorer.exe");
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            // Full path is needed otherwise it opens only an explorer window, without restoring the taskbar
            Process.Start(@"C:\Windows\explorer.exe");

            Console.WriteLine();
            OperationUtils.RemoveComponentUsingInstallWimTweakIfAllowed("Microsoft-Windows-OneDrive-Setup");

            ConsoleUtils.WriteLine("\nIf you get an error when launching system programs, just log out and log in again.", ConsoleColor.Cyan);
        }

        private void KillProcess(string processName)
        {
            ConsoleUtils.WriteLine($"Killing {processName}...", ConsoleColor.Green);
            SystemUtils.RunProcessSynchronously("taskkill", $"/F /IM {processName}");
        }

        private void RunOneDriveUninstaller()
        {
            string uninstallerPath = RetrieveOneDriveUninstallerPath();
            ConsoleUtils.WriteLine("Executing OneDrive uninstaller...", ConsoleColor.Green);
            int exitCode = SystemUtils.RunProcessSynchronously(uninstallerPath, "/uninstall");
            if (exitCode != 0)
                throw new Exception("OneDrive uninstaller terminated with non-zero status.");
        }

        private string RetrieveOneDriveUninstallerPath()
        {
            if (Env.Is64BitOperatingSystem)
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe";
            else
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\System32\OneDriveSetup.exe";
        }

        private void DisableOneDriveViaGroupPolicies()
        {
            ConsoleUtils.WriteLine("Disabling OneDrive via Group Policies...", ConsoleColor.Green);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
        }

        private void RemoveResidualFiles()
        {
            ConsoleUtils.WriteLine("Removing old files...", ConsoleColor.Green);
            SystemUtils.DeleteDirectoryIfExists(@"C:\OneDriveTemp", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", handleErrors: true);
        }

        private void RemoveResidualRegistryKeys()
        {
            ConsoleUtils.WriteLine("Deleting old registry keys...", ConsoleColor.Green);
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
        }
    }
}
