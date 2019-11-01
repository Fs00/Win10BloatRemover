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
            SystemUtils.KillProcess("onedrive");
            RunOneDriveUninstaller();
            DisableOneDriveViaGroupPolicies();

            SystemUtils.KillProcess("explorer");
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            Process.Start("explorer");

            Console.WriteLine();
            InstallWimTweak.RemoveComponentIfAllowed("Microsoft-Windows-OneDrive-Setup");
        }

        private void RunOneDriveUninstaller()
        {
            string uninstallerPath = RetrieveOneDriveUninstallerPath();
            Console.WriteLine("Executing OneDrive uninstaller...");
            int exitCode = SystemUtils.RunProcessSynchronouslyWithConsoleOutput(uninstallerPath, "/uninstall");
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
            Console.WriteLine("Disabling OneDrive via Group Policies...");
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\OneDrive");
            key.SetValue("DisableFileSyncNGSC", 1, RegistryValueKind.DWord);
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
