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
            OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-OneDrive-Setup");

            Console.WriteLine("If you get an error when launching system programs, just log out and log in again.");
        }

        private void KillProcess(string processName)
        {
            Console.WriteLine($"\nKilling {processName}...");
            ShellUtils.ExecuteWindowsCommand($"taskkill /F /IM {processName}");
        }

        private void RunOneDriveUninstaller()
        {
            string uninstallerPath = RetrieveOneDriveUninstallerPath();
            Console.WriteLine("Executing OneDrive uninstaller...");
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
            Console.WriteLine("\nDisabling OneDrive via Group Policies...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\OneDrive"))
                key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
        }

        private void RemoveResidualFiles()
        {
            Console.WriteLine("\nRemoving old files...");
            SystemUtils.DeleteDirectoryIfExists(@"C:\OneDriveTemp", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", handleErrors: true);
        }

        private void RemoveResidualRegistryKeys()
        {
            Console.WriteLine("\nDeleting old registry keys...");
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
        }
    }
}
