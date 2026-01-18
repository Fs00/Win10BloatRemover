using Microsoft.Win32;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;
using Env = System.Environment;

namespace Win10BloatRemover.Operations;

enum UwpAppRemovalMode
{
    CurrentUser,
    AllUsers
}

enum UwpAppGroup
{
    Bing,
    Calculator,
    Camera,
    Clock,
    CommunicationsApps,
    Copilot,
    Cortana,
    HelpAndFeedback,
    Maps,
    MediaPlayers,
    MixedReality,
    Mobile,
    OneNote,
    Paint3D,
    PhoneLink,
    Photos,
    Skype,
    SnipAndSketch,
    SolitaireCollection,
    SoundRecorder,
    StickyNotes,
    Store,
    Xbox
}

class UwpAppGroupRemover : IOperation
{
    private static readonly Dictionary<UwpAppGroup, string[]> appNamesForGroup = new() {
        { UwpAppGroup.Bing, [
            "Microsoft.BingNews",
            "Microsoft.BingWeather",
            "Microsoft.BingFinance",
            "Microsoft.BingSports",
            "Microsoft.BingSearch"
        ] },
        { UwpAppGroup.Calculator, ["Microsoft.WindowsCalculator"] },
        { UwpAppGroup.Camera, ["Microsoft.WindowsCamera"] },
        { UwpAppGroup.Clock, ["Microsoft.WindowsAlarms"] },
        { UwpAppGroup.CommunicationsApps, [
            "microsoft.windowscommunicationsapps",
            "Microsoft.People",
            "Microsoft.OutlookForWindows"
        ] },
        { UwpAppGroup.Copilot, ["Microsoft.Copilot", "Microsoft.MicrosoftOfficeHub"] },
        { UwpAppGroup.Cortana, ["Microsoft.549981C3F5F10"] },
        { UwpAppGroup.HelpAndFeedback, [
            "Microsoft.WindowsFeedbackHub",
            "Microsoft.GetHelp",
            "Microsoft.Getstarted"
        ] },
        { UwpAppGroup.Maps, ["Microsoft.WindowsMaps"] },
        { UwpAppGroup.MediaPlayers, ["Microsoft.ZuneMusic", "Microsoft.ZuneVideo"] },
        { UwpAppGroup.MixedReality, [
            "Microsoft.Microsoft3DViewer",
            "Microsoft.Print3D",
            "Microsoft.MixedReality.Portal"
        ] },
        { UwpAppGroup.Mobile, ["Microsoft.Messaging", "Microsoft.OneConnect"] },
        { UwpAppGroup.OneNote, ["Microsoft.Office.OneNote"] },
        { UwpAppGroup.Paint3D, ["Microsoft.MSPaint"] },
        { UwpAppGroup.PhoneLink, ["Microsoft.YourPhone", "MicrosoftWindows.CrossDevice"] },
        { UwpAppGroup.Photos, ["Microsoft.Windows.Photos"] },
        { UwpAppGroup.Skype, ["Microsoft.SkypeApp"] },
        { UwpAppGroup.SnipAndSketch, ["Microsoft.ScreenSketch"] },
        { UwpAppGroup.SolitaireCollection, ["Microsoft.MicrosoftSolitaireCollection"] },
        { UwpAppGroup.SoundRecorder, ["Microsoft.WindowsSoundRecorder"] },
        { UwpAppGroup.StickyNotes, ["Microsoft.MicrosoftStickyNotes"] },
        { UwpAppGroup.Store, ["Microsoft.WindowsStore", "Microsoft.StorePurchaseApp"] },
        { UwpAppGroup.Xbox, [
            "Microsoft.XboxGameCallableUI",
            "Microsoft.XboxSpeechToTextOverlay",
            "Microsoft.XboxApp",
            "Microsoft.XboxGameOverlay",
            "Microsoft.XboxGamingOverlay",
            "Microsoft.XboxIdentityProvider",
            "Microsoft.Xbox.TCUI",
            "Microsoft.GamingApp",
            "Microsoft.GamingServices"
        ] }
    };

    private readonly Dictionary<UwpAppGroup, Action> postUninstallOperationsForGroup;
    private readonly UwpAppGroup[] appsToRemove;
    private readonly UwpAppRemovalMode removalMode;
    private readonly IUserInterface ui;
    private readonly AppxRemover appxRemover;
    private readonly ServiceRemover serviceRemover;

    private readonly RebootRecommendedFlag rebootFlag = new RebootRecommendedFlag();
    public bool IsRebootRecommended => rebootFlag.IsRebootRecommended;

    public UwpAppGroupRemover(UwpAppGroup[] appsToRemove, UwpAppRemovalMode removalMode, IUserInterface ui,
                              AppxRemover appxRemover, ServiceRemover serviceRemover)
    {
        this.appsToRemove = appsToRemove;
        this.removalMode = removalMode;
        this.ui = ui;
        this.appxRemover = appxRemover;
        this.serviceRemover = serviceRemover;

        postUninstallOperationsForGroup = new Dictionary<UwpAppGroup, Action> {
            { UwpAppGroup.CommunicationsApps, () => {
                RemoveCommunicationsAppsServices();
                PreventAutomaticOutlookInstallation();
            } },
            { UwpAppGroup.Cortana, HideCortanaFromTaskBar },
            { UwpAppGroup.Maps, RemoveMapsServicesAndTasks },
            { UwpAppGroup.MixedReality, RemoveMixedRealityAppsLeftovers },
            { UwpAppGroup.Mobile, RemoveMessagingService },
            { UwpAppGroup.Paint3D, RemovePaint3DContextMenuEntries },
            { UwpAppGroup.Photos, RestoreWindowsPhotoViewer },
            { UwpAppGroup.Store, DisableStoreFeaturesAndServices },
            { UwpAppGroup.Xbox, DisableXboxFeaturesAndServices }
        };
    }

    public void Run()
    {
        foreach (UwpAppGroup appGroup in appsToRemove)
            UninstallAppsOfGroup(appGroup);
    }

    private void UninstallAppsOfGroup(UwpAppGroup appGroup)
    {
        string[] appsInGroup = appNamesForGroup[appGroup];
        ui.PrintHeading($"Removing {appGroup} {(appsInGroup.Length == 1 ? "app" : "apps")}...");

        (_, int failedRemovals) = removalMode == UwpAppRemovalMode.CurrentUser
            ? appxRemover.RemoveAppsForCurrentUser(appsInGroup)
            : appxRemover.RemoveAppsForAllUsers(appsInGroup);

        if (removalMode == UwpAppRemovalMode.AllUsers && failedRemovals == 0)
            TryPerformPostUninstallOperations(appGroup);
    }

    private void TryPerformPostUninstallOperations(UwpAppGroup appGroup)
    {
        try
        {
            if (postUninstallOperationsForGroup.ContainsKey(appGroup))
            {
                ui.PrintEmptySpace();
                postUninstallOperationsForGroup[appGroup]();
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
        Registry.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
    }

    private void RemoveMapsServicesAndTasks()
    {
        DisableScheduledTasks(@"\Microsoft\Windows\Maps\MapsUpdateTask", @"\Microsoft\Windows\Maps\MapsToastTask");
        RemoveServices("MapsBroker");
    }

    private void RemoveMessagingService()
    {
        RemoveServices("MessagingService");
    }

    private void RemovePaint3DContextMenuEntries()
    {
        ui.PrintMessage("Removing Paint 3D context menu entries...");
        OS.ExecuteWindowsPromptCommand(
            @"for /f ""tokens=1* delims="" %I in " +
             @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Edit"" ^| find /i ""3D Edit"" ') " +
            @"do (reg delete ""%I"" /f > nul)",
            ui
        );
    }

    private void RemoveMixedRealityAppsLeftovers()
    {
        RemoveServices("MixedRealityOpenXRSvc", "perceptionsimulation", "spectrum", "SharedRealitySvc", "VacSvc");
        Remove3DObjectsFolder();
        Remove3DPrintContextMenuEntries();
    }

    private void Remove3DObjectsFolder()
    {
        ui.PrintMessage("Removing 3D Objects folder...");
        using RegistryKey key = Registry.LocalMachine64.OpenSubKeyWritable(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace"
        );
        key.DeleteSubKeyTree("{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", throwOnMissingSubKey: false);

        OS.TryDeleteDirectoryIfExists($@"{Env.GetFolderPath(Env.SpecialFolder.UserProfile)}\3D Objects", ui);
    }

    private void Remove3DPrintContextMenuEntries()
    {
        ui.PrintMessage("Removing 3D Print context menu entries...");
        OS.ExecuteWindowsPromptCommand(
            @"for /f ""tokens=1* delims="" %I in " +
            @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Print"" ^| find /i ""3D Print"" ') " +
            @"do (reg delete ""%I"" /f > nul)",
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

        string[] imageTypes = ["Paint.Picture", "giffile", "jpegfile", "pngfile"];
        foreach (string type in imageTypes)
        {
            Registry.SetValue(
                $@"HKEY_CLASSES_ROOT\{type}\shell\open\command", valueName: null,
                PHOTO_VIEWER_SHELL_COMMAND, RegistryValueKind.ExpandString
            );
            Registry.SetValue($@"HKEY_CLASSES_ROOT\{type}\shell\open\DropTarget", "Clsid", PHOTO_VIEWER_CLSID);
        }
    }

    private void RemoveCommunicationsAppsServices()
    {
        RemoveServices("PimIndexMaintenanceSvc");
        RemoveFeatures("OneCoreUAP.OneSync");
    }

    private void PreventAutomaticOutlookInstallation()
    {
        ui.PrintMessage("Blocking automatic installation of the new Outlook app via registry edits...");
        using RegistryKey key = Registry.LocalMachine64.CreateSubKey(
            @"SOFTWARE\Microsoft\WindowsUpdate\Orchestrator\UScheduler_Oobe"
        );
        key.SetValue("BlockedOobeUpdaters", """["MS_Outlook"]""");
    }

    private void DisableStoreFeaturesAndServices()
    {
        ui.PrintMessage("Disabling Microsoft Store features...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsStore", "RemoveWindowsStore", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\PushToInstall", "DisablePushToInstall", 1);
        Registry.SetForCurrentAndDefaultUser(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            "SilentInstalledAppsEnabled", 0
        );
        rebootFlag.SetRecommended();

        RemoveServices("PushToInstall");
    }
    
    private void DisableXboxFeaturesAndServices()
    {
        DisableScheduledTasks(@"Microsoft\XblGameSave\XblGameSaveTask");
        RemoveServices("XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc", "BcastDVRUserService");

        ui.PrintMessage("Disabling Xbox Game Bar...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
        rebootFlag.SetRecommended();
        
        HideGameBarSettings();
    }

    private void HideGameBarSettings()
    {
        using var explorerPoliciesKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
        // We don't want to overwrite the settings visibility policy in case the user has already set it for their own purposes
        if (explorerPoliciesKey.HasValue("SettingsPageVisibility"))
            return;

        ui.PrintMessage("Hiding Game Bar and capture settings in the Settings app...");
        explorerPoliciesKey.SetValue("SettingsPageVisibility", "hide:gaming-gamebar;gaming-gamedvr");
    }

    private void DisableScheduledTasks(params string[] scheduledTasks)
    {
        new ScheduledTasksDisabler(scheduledTasks, ui).Run();
    }

    private void RemoveServices(params string[] services)
    {
        serviceRemover.BackupAndRemove(services);
        rebootFlag.UpdateIfNeeded(serviceRemover.IsRebootRecommended);
    }
    
    private void RemoveFeatures(params string[] features)
    {
        var featuresRemover = new FeaturesRemover(features, ui);
        featuresRemover.Run();
        rebootFlag.UpdateIfNeeded(featuresRemover.IsRebootRecommended);
    }
}
