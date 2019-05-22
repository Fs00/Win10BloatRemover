using Microsoft.Win32;
using System;
using System.IO;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    class OneDriveRemover : IOperation
    {
        public void PerformTask()
        {
            KillOneDriveProcess();
            string uninstallerPath = RetrieveOneDriveUninstallerPath();
            RunOneDriveUninstaller(uninstallerPath);
            RemoveResidualFiles();
            RemoveResidualRegistryKeys();
            Console.WriteLine("Some folders may not exist, it's normal.");
        }

        private void KillOneDriveProcess()
        {
            Console.WriteLine("Killing OneDrive process...");
            SystemUtils.ExecuteWindowsCommand("taskkill /F /IM onedrive.exe");
        }

        private string RetrieveOneDriveUninstallerPath()
        {
            if (Env.Is64BitOperatingSystem)
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe";
            else
                return $@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\System32\OneDriveSetup.exe";
        }

        private void RunOneDriveUninstaller(string uninstallerPath)
        {
            Console.WriteLine("Executing OneDrive uninstaller...");
            using (var oneDriveUninstallProcess = SystemUtils.RunProcess(uninstallerPath, "/uninstall"))
            {
                oneDriveUninstallProcess.PrintOutputAndErrors();
                oneDriveUninstallProcess.WaitForExit();

                if (oneDriveUninstallProcess.ExitCode != 0)
                    throw new Exception("OneDrive uninstaller terminated with non-zero status.");
            }
        }

        private void RemoveResidualFiles()
        {
            Console.WriteLine("Removing old files...");
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists(@"C:\OneDriveTemp", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", handleErrors: true);
            SystemUtils.DeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.CommonApplicationData)}\Microsoft\OneDrive", handleErrors: true);

            try
            {
                string oneDriveStandaloneUpdater = $@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\" +
                                                    @"Microsoft\OneDrive\OneDriveStandaloneUpdater.exe";
                if (File.Exists(oneDriveStandaloneUpdater))
                    File.Delete(oneDriveStandaloneUpdater);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"An error occurred while deleting OneDrive standalone updater: {exc.Message}");
            }
        }

        private void RemoveResidualRegistryKeys()
        {
            Console.WriteLine("Deleting old registry keys...");
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID", true))
                key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", false);
        }
    }
}
