using Microsoft.Win32;
using System;
using System.Collections.Generic;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations
{
    public enum UwpAppRemovalMode
    {
        CurrentUser,
        AllUsers
    }

    public enum UwpAppGroup
    {
        AlarmsAndClock,
        Bing,               // Weather, News, Finance and Sports
        Calculator,
        Camera,
        CommunicationsApps,
        Cortana,
        EdgeUWP,
        HelpAndFeedback,
        Maps,
        Messaging,
        MixedReality,       // 3D Viewer, Print 3D and Mixed Reality Portal
        Mobile,             // Your Phone and Mobile plans (aka OneConnect)
        OfficeHub,
        OneNote,
        Paint3D,
        Photos,
        Skype,
        SnipAndSketch,
        SolitaireCollection,
        SoundRecorder,
        StickyNotes,
        Store,
        Xbox,
        Zune                // Groove Music and Movies
    }

    public class UwpAppGroupRemover : IOperation
    {
        // This dictionary contains the exact apps names corresponding to every defined group
        private static readonly Dictionary<UwpAppGroup, string[]> appNamesForGroup = new Dictionary<UwpAppGroup, string[]> {
            { UwpAppGroup.AlarmsAndClock, new[] { "Microsoft.WindowsAlarms" } },
            { UwpAppGroup.Bing, new[] {
                "Microsoft.BingNews",
                "Microsoft.BingWeather",
                "Microsoft.BingFinance",
                "Microsoft.BingSports"
            } },
            { UwpAppGroup.Calculator, new[] { "Microsoft.WindowsCalculator" } },
            { UwpAppGroup.Camera, new[] { "Microsoft.WindowsCamera" } },
            { UwpAppGroup.CommunicationsApps, new[] { "microsoft.windowscommunicationsapps", "Microsoft.People" } },
            { UwpAppGroup.Cortana, new[] { "Microsoft.549981C3F5F10" } },
            { UwpAppGroup.EdgeUWP, new[] { "Microsoft.MicrosoftEdge", "Microsoft.MicrosoftEdgeDevToolsClient" } },
            { UwpAppGroup.HelpAndFeedback, new[] {
                "Microsoft.WindowsFeedbackHub",
                "Microsoft.GetHelp",
                "Microsoft.Getstarted"
            } },
            { UwpAppGroup.Maps, new[] { "Microsoft.WindowsMaps" } },
            { UwpAppGroup.Messaging, new[] { "Microsoft.Messaging" } },
            { UwpAppGroup.MixedReality, new[] {
                "Microsoft.Microsoft3DViewer",
                "Microsoft.Print3D",
                "Microsoft.MixedReality.Portal"
            } },
            { UwpAppGroup.Mobile, new[] { "Microsoft.YourPhone", "Microsoft.OneConnect" } },
            { UwpAppGroup.OfficeHub, new[] { "Microsoft.MicrosoftOfficeHub" } },
            { UwpAppGroup.OneNote, new[] { "Microsoft.Office.OneNote" } },
            { UwpAppGroup.Paint3D, new[] { "Microsoft.MSPaint" } },
            { UwpAppGroup.Photos, new[] { "Microsoft.Windows.Photos" } },
            { UwpAppGroup.Skype, new[] { "Microsoft.SkypeApp" } },
            { UwpAppGroup.SnipAndSketch, new[] { "Microsoft.ScreenSketch" } },
            { UwpAppGroup.SolitaireCollection, new[] { "Microsoft.MicrosoftSolitaireCollection" } },
            { UwpAppGroup.SoundRecorder, new[] { "Microsoft.WindowsSoundRecorder" } },
            { UwpAppGroup.StickyNotes, new[] { "Microsoft.MicrosoftStickyNotes" } },
            { UwpAppGroup.Store, new[] {
                "Microsoft.WindowsStore",
                "Microsoft.StorePurchaseApp",
                "Microsoft.Services.Store.Engagement",
            } },
            { UwpAppGroup.Xbox, new[] {
                "Microsoft.XboxGameCallableUI",
                "Microsoft.XboxSpeechToTextOverlay",
                "Microsoft.XboxApp",
                "Microsoft.XboxGameOverlay",
                "Microsoft.XboxGamingOverlay",
                "Microsoft.XboxIdentityProvider",
                "Microsoft.Xbox.TCUI"
            } },
            { UwpAppGroup.Zune, new[] { "Microsoft.ZuneMusic", "Microsoft.ZuneVideo" } }
        };

        private readonly Dictionary<UwpAppGroup, Action> postUninstallOperationsForGroup;
        private readonly UwpAppGroup[] appsToRemove;
        private readonly UwpAppRemovalMode removalMode;
        private readonly IUserInterface ui;
        private readonly AppxRemover appxRemover;
        private readonly ServiceRemover serviceRemover;

        private int totalRemovedApps = 0;

        public bool IsRebootRecommended { get; private set; }

        #nullable disable warnings
        public UwpAppGroupRemover(UwpAppGroup[] appsToRemove, UwpAppRemovalMode removalMode, IUserInterface ui,
                             AppxRemover appxRemover, ServiceRemover serviceRemover)
        {
            this.appsToRemove = appsToRemove;
            this.removalMode = removalMode;
            this.ui = ui;
            this.appxRemover = appxRemover;
            this.serviceRemover = serviceRemover;

            postUninstallOperationsForGroup = new Dictionary<UwpAppGroup, Action> {
                { UwpAppGroup.CommunicationsApps, RemoveOneSyncServiceFeature },
                { UwpAppGroup.Cortana, HideCortanaFromTaskBar },
                { UwpAppGroup.Maps, RemoveMapsServicesAndTasks },
                { UwpAppGroup.Messaging, RemoveMessagingService },
                { UwpAppGroup.Paint3D, RemovePaint3DContextMenuEntries },
                { UwpAppGroup.Photos, RestoreWindowsPhotoViewer },
                { UwpAppGroup.MixedReality, RemoveMixedRealityAppsLeftovers },
                { UwpAppGroup.Xbox, RemoveXboxServicesAndTasks },
                { UwpAppGroup.Store, DisableStoreFeaturesAndServices }
            };
        }
        #nullable restore warnings

        public void Run()
        {
            foreach (UwpAppGroup appGroup in appsToRemove)
                UninstallAppsOfGroup(appGroup);

            if (totalRemovedApps > 0)
                RestartExplorer();
        }

        private void UninstallAppsOfGroup(UwpAppGroup appGroup)
        {
            string[] appsInGroup = appNamesForGroup[appGroup];
            ui.PrintHeading($"Removing {appGroup} {(appsInGroup.Length == 1 ? "app" : "apps")}...");

            var result = removalMode switch {
                UwpAppRemovalMode.CurrentUser => appxRemover.RemoveAppsForCurrentUser(appsInGroup),
                UwpAppRemovalMode.AllUsers => appxRemover.RemoveAppsForAllUsers(appsInGroup)
            };

            totalRemovedApps += result.RemovedApps;

            if (removalMode == UwpAppRemovalMode.AllUsers && result.FailedRemovals == 0)
                TryPerformPostUninstallOperations(appGroup);
        }

        private void RestartExplorer()
        {
            ui.PrintHeading("Restarting Explorer to avoid stale app entries in Start menu...");
            OS.CloseExplorer();
            OS.StartExplorer();
        }

        private void TryPerformPostUninstallOperations(UwpAppGroup appGroup)
        {
            try
            {
                if (postUninstallOperationsForGroup.ContainsKey(appGroup))
                {
                    ui.PrintEmptySpace();
                    postUninstallOperationsForGroup[appGroup]();
                    IsRebootRecommended = true;
                }
            }
            catch (Exception exc)
            {
                ui.PrintError($"An error occurred while performing post-uninstall/cleanup operations: {exc.Message}");
            }
        }

        private void HideCortanaFromTaskBar()
        {
            ui.PrintMessage("Hiding Cortana from the taskbar of current and default user...");
            RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
        }

        private void RemoveMapsServicesAndTasks()
        {
            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Maps\MapsUpdateTask",
                @"\Microsoft\Windows\Maps\MapsToastTask"
            }, ui).Run();
            serviceRemover.BackupAndRemove("MapsBroker", "lfsvc");
        }

        private void RemoveXboxServicesAndTasks()
        {
            new ScheduledTasksDisabler(new[] { @"Microsoft\XblGameSave\XblGameSaveTask" }, ui).Run();
            serviceRemover.BackupAndRemove("XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc");
            ui.PrintMessage("Disabling Xbox Game Bar...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
        }

        private void RemoveMessagingService()
        {
            serviceRemover.BackupAndRemove("MessagingService");
        }

        private void RemovePaint3DContextMenuEntries()
        {
            ui.PrintMessage("Removing Paint 3D context menu entries...");
            OS.ExecuteWindowsPromptCommand(
                @"echo off & for /f ""tokens=1* delims="" %I in " +
                 @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Edit"" ^| find /i ""3D Edit"" ') " +
                @"do (reg delete ""%I"" /f )",
                ui
            );
        }

        private void RemoveMixedRealityAppsLeftovers()
        {
            Remove3DObjectsFolder();
            Remove3DPrintContextMenuEntries();
        }

        private void Remove3DObjectsFolder()
        {
            ui.PrintMessage("Removing 3D Objects folder...");
            using RegistryKey key = RegistryUtils.LocalMachine64.OpenSubKeyWritable(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace"
            );
            key.DeleteSubKeyTree("{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", throwOnMissingSubKey: false);

            OS.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\3D Objects", ui);
        }

        private void Remove3DPrintContextMenuEntries()
        {
            ui.PrintMessage("Removing 3D Print context menu entries...");
            OS.ExecuteWindowsPromptCommand(
                @"echo off & for /f ""tokens=1* delims="" %I in " +
                @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Print"" ^| find /i ""3D Print"" ') " +
                @"do (reg delete ""%I"" /f )",
                ui
            );
        }

        private void RestoreWindowsPhotoViewer()
        {
            ui.PrintMessage("Setting file association with legacy photo viewer for BMP, GIF, JPEG, PNG and TIFF pictures...");

            const string PHOTO_VIEWER_SHELL_COMMAND =
                @"%SystemRoot%\System32\rundll32.exe ""%ProgramFiles%\Windows Photo Viewer\PhotoViewer.dll"", ImageView_Fullscreen %1";
            const string PHOTO_VIEWER_CLSID = "{FFE2A43C-56B9-4bf5-9A79-CC6D4285608A}";

            Registry.SetValue(@"HKEY_CLASSES_ROOT\Applications\photoviewer.dll\shell\open", "MuiVerb", "@photoviewer.dll,-3043");
            Registry.SetValue(
                @"HKEY_CLASSES_ROOT\Applications\photoviewer.dll\shell\open\command", valueName: null,
                PHOTO_VIEWER_SHELL_COMMAND, RegistryValueKind.ExpandString
            );
            Registry.SetValue(@"HKEY_CLASSES_ROOT\Applications\photoviewer.dll\shell\open\DropTarget", "Clsid", PHOTO_VIEWER_CLSID);

            string[] imageTypes = { "Paint.Picture", "giffile", "jpegfile", "pngfile" };
            foreach (string type in imageTypes)
            {
                Registry.SetValue(
                    $@"HKEY_CLASSES_ROOT\{type}\shell\open\command", valueName: null,
                    PHOTO_VIEWER_SHELL_COMMAND, RegistryValueKind.ExpandString
                );
                Registry.SetValue($@"HKEY_CLASSES_ROOT\{type}\shell\open\DropTarget", "Clsid", PHOTO_VIEWER_CLSID);
            }
        }

        private void RemoveOneSyncServiceFeature()
        {
            new FeaturesRemover(new[] { "OneCoreUAP.OneSync" }, ui).Run();
        }

        private void DisableStoreFeaturesAndServices()
        {
            ui.PrintMessage("Disabling Microsoft Store features...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsStore", "RemoveWindowsStore", 1);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\PushToInstall", "DisablePushToInstall", 1);
            RegistryUtils.SetForCurrentAndDefaultUser(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SilentInstalledAppsEnabled", 0
            );

            serviceRemover.BackupAndRemove("PushToInstall");
        }
    }
}
