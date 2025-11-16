using Microsoft.Win32;
using System.IO;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations;

class OneDriveRemover(IUserInterface ui) : IOperation
{
    public void Run()
    {
        KillOneDriveProcesses();
        DisableOneDrive();
        RunOneDriveUninstaller();
        RemoveOneDriveLeftovers();
        DisableAutomaticSetupForNewUsers();
    }

    private void KillOneDriveProcesses()
    {
        ui.PrintMessage("Shutting down OneDrive processes...");
        OS.KillProcess("onedrive");
        OS.KillProcess("onedrive.sync.service");
    }

    private void DisableOneDrive()
    {
        ui.PrintMessage("Disabling OneDrive via registry edits...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
        using RegistryKey key = RegistryUtils.LocalMachine64.CreateSubKey(@"SOFTWARE\Microsoft\OneDrive");
        key.SetValue("PreventNetworkTrafficPreUserSignIn", 1);
    }

    private void RunOneDriveUninstaller()
    {
        ui.PrintMessage("Executing OneDrive uninstaller...");
        string setupPath = RetrieveOneDriveSetupPath();
        var uninstallationExitCode = OS.RunProcessBlockingWithOutput(setupPath, "/uninstall", ui);
        if (uninstallationExitCode.IsNotSuccessful())
        {
            ui.PrintError("Uninstallation failed due to an unknown error.");
            ui.ThrowIfUserDenies("Do you still want to continue the process by removing all leftover OneDrive " +
                                 "files (including its application files for the current user) and registry keys?");
        }
    }

    private string RetrieveOneDriveSetupPath()
    {
        FileInfo[] potentialOneDriveSetupLocations = [
            new FileInfo($@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\SysWOW64\OneDriveSetup.exe"),
            // "Sysnative" is used to prevent automatic redirection from System32 to SysWOW64 folder
            // See https://learn.microsoft.com/en-us/windows/win32/winprog64/file-system-redirector
            new FileInfo($@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\Sysnative\OneDriveSetup.exe"),
            // The Sysnative alias apparently doesn't work on 32-bit Windows installations
            new FileInfo($@"{Env.GetFolderPath(Env.SpecialFolder.Windows)}\System32\OneDriveSetup.exe")
        ];
        var foundOneDriveSetup = potentialOneDriveSetupLocations.FirstOrDefault(file => file.Exists);
        if (foundOneDriveSetup == null)
            throw new Exception("OneDrive uninstaller was not found on the system.");
        return foundOneDriveSetup.FullName;
    }

    private void RemoveOneDriveLeftovers()
    {
        ui.PrintMessage("Removing OneDrive leftovers...");
        OS.CloseExplorer();
        RemoveResidualFiles();
        RemoveResidualRegistryKeys();
        OS.StartExplorer();
    }

    private void RemoveResidualFiles()
    {
        OS.TryDeleteDirectoryIfExists(@"C:\OneDriveTemp", ui);
        OS.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\OneDrive", ui);
        OS.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.LocalApplicationData)}\Microsoft\OneDrive", ui);
        OS.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\OneDrive", ui);
        var menuShortcut = new FileInfo($@"{Env.GetFolderPath(Env.SpecialFolder.StartMenu)}\Programs\OneDrive.lnk");
        if (menuShortcut.Exists)
            menuShortcut.Delete();
    }

    private void RemoveResidualRegistryKeys()
    {
        using RegistryKey classesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
        using RegistryKey key = classesRoot.OpenSubKeyWritable(@"CLSID");
        key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
    }

    // Borrowed from github.com/W4RH4WK/Debloat-Windows-10/blob/master/scripts/remove-onedrive.ps1
    private void DisableAutomaticSetupForNewUsers()
    {
        ui.PrintMessage("Disabling automatic OneDrive setup for new users...");
        RegistryUtils.DefaultUser.DeleteSubKeyValue(@"Software\Microsoft\Windows\CurrentVersion\Run", "OneDriveSetup");
    }
}
