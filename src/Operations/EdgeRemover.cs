using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Win10BloatRemover.Utils;
using static System.Environment;
using Version = System.Version;

namespace Win10BloatRemover.Operations
{
    public class EdgeRemover : IOperation
    {
        private readonly IUserInterface ui;
        private readonly IOperation legacyEdgeRemover;

        public EdgeRemover(IUserInterface ui, IOperation legacyEdgeRemover)
        {
            this.ui = ui;
            this.legacyEdgeRemover = legacyEdgeRemover;
        }

        public void Run()
        {
            UninstallEdgeChromium();
            legacyEdgeRemover.Run();
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

            // Recent versions of Edge Update (the latest at the moment of writing is 1.3.171.37) set a flag
            // that blocks the uninstallation of Edge inside the following registry value.
            // If we don't remove it, the uninstaller will simply do nothing.
            Registry.LocalMachine.DeleteSubKeyValue(
                @"SOFTWARE\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}",
                "experiment_control_labels");

            ui.PrintMessage("Running uninstaller...");
            OS.RunProcessBlocking(installerPath, "--uninstall --force-uninstall --msedge --system-level --verbose-logging");
            // Since part of the uninstallation happens in another process launched by the installer,
            // we want to let this process do its work before continuing
            WaitForEdgeUninstallation(installerPath);

            RemoveEdgeLeftovers();
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
}
