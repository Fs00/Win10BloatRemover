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
        DisableOneDrive();
        UninstallOneDriveForCurrentUser();
        RemoveOneDriveLeftovers();
        DisableAutomaticSetupForNewUsers();
    }

    private void DisableOneDrive()
    {
        ui.PrintMessage("Disabling OneDrive via registry edits...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC", 1);
        using RegistryKey key = Registry.LocalMachine64.CreateSubKey(@"SOFTWARE\Microsoft\OneDrive");
        key.SetValue("PreventNetworkTrafficPreUserSignIn", 1);
    }

    private void UninstallOneDriveForCurrentUser()
    {
        ui.PrintMessage("Executing OneDrive uninstaller...");
        string? uninstallCommand = RetrieveOneDriveUninstallCommand();
        if (uninstallCommand == null)
        {
            ui.PrintNotice("OneDrive does not appear to be installed for the current user.");
            ui.ThrowIfUserDenies("Do you still want to proceed and remove any leftover OneDrive files and registry keys?");
            return;
        }
        
        OS.ExecuteWindowsPromptCommand(uninstallCommand, ui);
        // We cannot rely on the exit code of the OneDrive uninstaller to determine if the uninstallation was completed
        // successfully, as it always exits with a non-zero status regardless of the outcome.
        bool isOneDriveStillInstalled = RetrieveOneDriveUninstallCommand() != null;
        if (isOneDriveStillInstalled)
        {
            ui.PrintError("It seems that OneDrive is still installed despite our attempt to remove it.\n" +
                          "Try again, and if the error persists, you might want to take a look at the\n" +
                          @"OneDrive setup logs in %LOCALAPPDATA%\Microsoft\OneDrive\setup\logs.");
            ui.PrintEmptySpace();
            throw new Exception("OneDrive uninstallation did not complete successfully.");
        }
    }

    private string? RetrieveOneDriveUninstallCommand()
    {
        using var oneDriveUninstallKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OneDriveSetup.exe");
        if (oneDriveUninstallKey == null)
            return null;
        
        ui.PrintMessage($"Detected OneDrive version {oneDriveUninstallKey.GetValue("DisplayVersion")}.");
        return (string?) oneDriveUninstallKey.GetValue("UninstallString");
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
        using RegistryKey key = classesRoot.OpenSubKeyWritable("CLSID");
        key.DeleteSubKeyTree("{018D5C66-4533-4307-9B53-224DE2ED1FE6}", throwOnMissingSubKey: false);
    }

    // Borrowed from github.com/W4RH4WK/Debloat-Windows-10/blob/master/scripts/remove-onedrive.ps1
    private void DisableAutomaticSetupForNewUsers()
    {
        ui.PrintMessage("Disabling automatic OneDrive setup for new users...");
        Registry.DefaultUser.DeleteSubKeyValue(@"Software\Microsoft\Windows\CurrentVersion\Run", "OneDriveSetup");
    }
}
