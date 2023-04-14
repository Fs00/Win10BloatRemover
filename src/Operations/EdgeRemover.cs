using Microsoft.Win32;
using System.IO;
using System.Threading;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;
using static System.Environment;
using Version = System.Version;

namespace Win10BloatRemover.Operations;

public class EdgeRemover : IOperation
{
    private readonly IUserInterface ui;
    private readonly AppxRemover appxRemover;

    public EdgeRemover(IUserInterface ui, AppxRemover appxRemover)
    {
        this.ui = ui;
        this.appxRemover = appxRemover;
    }

    public void Run()
    {
        UninstallEdgeChromium();
        UninstallLegacyEdgeApps();
    }

    private void UninstallEdgeChromium()
    {
        ui.PrintHeading("Removing Edge Chromium...");
        string? installerPath = RetrieveEdgeChromiumInstallerPath();
        if (installerPath == null)
        {
            ui.PrintMessage("Edge Chromium is not installed.");
            return;
        }

        RemoveEdgeUninstallationBlockers();

        ui.PrintMessage("Running uninstaller...");
        OS.RunProcessBlocking(installerPath, "--uninstall --force-uninstall --msedge --system-level --verbose-logging");
        // Since part of the uninstallation happens in another process launched by the installer,
        // we want to let this process do its work before continuing
        WaitForEdgeUninstallation(installerPath);

        RemoveEdgeLeftovers();
    }

    private void UninstallLegacyEdgeApps()
    {
        ui.PrintHeading("Removing legacy Edge...");
        appxRemover.RemoveAppsForAllUsers("Microsoft.MicrosoftEdge", "Microsoft.MicrosoftEdgeDevToolsClient");
    }

    private string? RetrieveEdgeChromiumInstallerPath()
    {
        string edgeChromiumBaseFolder = $@"{GetFolderPath(SpecialFolder.ProgramFilesX86)}\Microsoft\Edge\Application";
        if (!Directory.Exists(edgeChromiumBaseFolder))
            return null;

        Version? edgeChromiumVersion = null;
        string? folderOfSpecificEdgeVersion = Directory.EnumerateDirectories(edgeChromiumBaseFolder)
            .FirstOrDefault(folder => Version.TryParse(new DirectoryInfo(folder).Name, out edgeChromiumVersion));

        if (folderOfSpecificEdgeVersion != null)
        {
            string installerPath = $@"{folderOfSpecificEdgeVersion}\Installer\setup.exe";
            if (File.Exists(installerPath))
            {
                ui.PrintMessage($"Detected Edge Chromium version {edgeChromiumVersion}.");
                return installerPath;
            }
        }
        return null;
    }

    private void RemoveEdgeUninstallationBlockers()
    {
        // Some versions of Edge Update set a flag inside the following registry value that blocks Edge uninstallation.
        // If we don't remove it, the uninstaller will simply do nothing.
        Registry.LocalMachine.DeleteSubKeyValue(
            @"SOFTWARE\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}",
            "experiment_control_labels");

        // Latest versions of Edge (112 and newer for sure, maybe some previous ones too) don't check the
        // above registry value, but they refuse to uninstall themselves if the following one is missing.
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\EdgeUpdateDev", "AllowUninstall", 1);
    }

    private void WaitForEdgeUninstallation(string edgeInstallerPath)
    {
        int waitedSeconds = 0;
        var waitInterval = TimeSpan.FromSeconds(5);
        while (waitedSeconds < 10)
        {
            Thread.Sleep(waitInterval);
            bool edgeFilesRemoved = !File.Exists(edgeInstallerPath);
            if (edgeFilesRemoved)
                return;

            waitedSeconds += waitInterval.Seconds;
        }

        ui.PrintError("It seems that Edge Chromium is still installed despite our attempts to remove it.\n" +
                      "Try again, and if this error still happens, you might want to report an issue on GitHub or check\n" +
                      @"the Edge installer log in your %TEMP% directory.");
        ui.PrintEmptySpace();
        throw new Exception("Edge Chromium uninstallation wasn't completed successfully or took too long.");
    }

    private void RemoveEdgeLeftovers()
    {
        ui.PrintMessage("Removing leftover data for the current user...");
        OS.TryDeleteDirectoryIfExists($@"{GetFolderPath(SpecialFolder.UserProfile)}\MicrosoftEdgeBackups", ui);
        OS.TryDeleteDirectoryIfExists($@"{GetFolderPath(SpecialFolder.LocalApplicationData)}\MicrosoftEdge", ui);
        OS.TryDeleteDirectoryIfExists($@"{GetFolderPath(SpecialFolder.LocalApplicationData)}\Microsoft\Edge", ui);
        var desktopShortcut = new FileInfo($@"{GetFolderPath(SpecialFolder.DesktopDirectory)}\Microsoft Edge.lnk");
        if (desktopShortcut.Exists)
            desktopShortcut.Delete();
    }
}
