using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public enum UWPAppRemovalMode {
        KeepProvisionedPackages,
        RemoveProvisionedPackages
    }

    public enum UWPAppGroup
    {
        Bing,               // Weather and News
        Mobile,             // YourPhone, OneConnect (aka Mobile plans) and Connect app
        Xbox,
        OfficeHub,
        OneNote,
        Camera,
        HelpAndFeedback,
        Maps,
        Zune,               // Groove Music and Movies
        People,
        MailAndCalendar,
        Messaging,
        SolitaireCollection,
        StickyNotes,
        MixedReality,       // 3D Viewer, Print 3D and Mixed Reality Portal
        Paint3D,
        Skype,
        Photos,
        AlarmsAndClock,
        Calculator,
        SnipAndSketch,
        Store,
        Edge
    }

    class UWPAppRemover : IOperation
    {
        // This dictionary contains the exact apps names corresponding to every group in the enum
        private static readonly Dictionary<UWPAppGroup, string[]> appNamesForGroup = new Dictionary<UWPAppGroup, string[]> {
            { UWPAppGroup.AlarmsAndClock, new[] { "Microsoft.WindowsAlarms" } },
            { UWPAppGroup.Bing, new[] { "Microsoft.BingNews", "Microsoft.BingWeather" } },
            { UWPAppGroup.Calculator, new[] { "Microsoft.WindowsCalculator" } },
            { UWPAppGroup.Camera, new[] { "Microsoft.WindowsCamera" } },
            { UWPAppGroup.Edge, new[] { "Microsoft.MicrosoftEdge", "Microsoft.MicrosoftEdgeDevToolsClient" } },
            { UWPAppGroup.HelpAndFeedback, new[] {
                    "Microsoft.WindowsFeedbackHub",
                    "Microsoft.GetHelp",
                    "Microsoft.Getstarted"
                }
            },
            { UWPAppGroup.MailAndCalendar, new[] { "microsoft.windowscommunicationsapps" } },
            { UWPAppGroup.Maps, new[] { "Microsoft.WindowsMaps" } },
            { UWPAppGroup.Messaging, new[] { "Microsoft.Messaging" } },
            { UWPAppGroup.MixedReality, new[] {
                    "Microsoft.Microsoft3DViewer",
                    "Microsoft.Print3D",
                    "Microsoft.MixedReality.Portal"
                }
            },
            { UWPAppGroup.Mobile, new[] { "Microsoft.YourPhone", "Microsoft.OneConnect" } },
            { UWPAppGroup.OfficeHub, new[] { "Microsoft.MicrosoftOfficeHub" } },
            { UWPAppGroup.OneNote, new[] { "Microsoft.Office.OneNote" } },
            { UWPAppGroup.Paint3D, new[] { "Microsoft.MSPaint" } },
            { UWPAppGroup.People, new[] { "Microsoft.People" } },
            { UWPAppGroup.Photos, new[] { "Microsoft.Windows.Photos" } },
            { UWPAppGroup.Skype, new[] { "Microsoft.SkypeApp" } },
            { UWPAppGroup.SnipAndSketch, new[] { "Microsoft.SkreenSketch" } },
            { UWPAppGroup.SolitaireCollection, new[] { "Microsoft.MicrosoftSolitaireCollection" } },
            { UWPAppGroup.StickyNotes, new[] { "Microsoft.MicrosoftStickyNotes" } },
            { UWPAppGroup.Store, new[] {
                    "Microsoft.WindowsStore",
                    "Microsoft.StorePurchaseApp",
                    "Microsoft.Services.Store.Engagement",
                }
            },
            { UWPAppGroup.Xbox, new[] {
                    "Microsoft.XboxGameCallableUI",
                    "Microsoft.XboxSpeechToTextOverlay",
                    "Microsoft.XboxApp",
                    "Microsoft.XboxGameOverlay",
                    "Microsoft.XboxGamingOverlay",
                    "Microsoft.XboxIdentityProvider",
                    "Microsoft.Xbox.TCUI"
                }
            },
            { UWPAppGroup.Zune, new[] {"Microsoft.ZuneMusic", "Microsoft.ZuneVideo" } }
        };

        private static readonly Dictionary<UWPAppGroup, Action> postUninstallOperationsForGroup = new Dictionary<UWPAppGroup, Action> {
            { UWPAppGroup.Edge, RemoveEdgeResidualFiles },
            {
                UWPAppGroup.Mobile,
                () => OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-PPIProjection-Package")   // Connect app
            },
            {
                UWPAppGroup.HelpAndFeedback,
                () => OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-ContactSupport")
            },
            { UWPAppGroup.Maps, RemoveMapsServicesAndTasks },
            { UWPAppGroup.Messaging, RemoveMessagingService },
            { UWPAppGroup.Paint3D, RemovePaint3DContextMenuEntries },
            { UWPAppGroup.MixedReality, RemovePrint3DContextMenuEntries },
            { UWPAppGroup.Xbox, RemoveXboxServicesAndTasks },
            { UWPAppGroup.MailAndCalendar, RemoveMailAndPeopleService },
            { UWPAppGroup.People, RemoveMailAndPeopleService },
            { UWPAppGroup.Store, RemoveStoreFeaturesAndServices }
        };

        private readonly UWPAppGroup[] appsToRemove;
        private readonly UWPAppRemovalMode removalMode;

        public UWPAppRemover(UWPAppGroup[] appsToRemove, UWPAppRemovalMode removalMode)
        {
            this.appsToRemove = appsToRemove;
            this.removalMode = removalMode;
        }

        public void PerformTask()
        {
            using (PowerShell psInstance = PowerShell.Create())
            {
                foreach (UWPAppGroup appGroup in appsToRemove)
                {
                    ConsoleUtils.WriteLine($"Removing {appGroup.ToString()} app(s)...", ConsoleColor.Green);

                    bool atLeastOneAppUninstalled = false;
                    foreach (string appName in appNamesForGroup[appGroup])
                    {
                        // The following script uninstalls the specified app package for all users when it is found
                        // and removes the package from Windows image (so that new users don't find the removed app)
                        string appRemovalScript =
                            $"$package = Get-AppxPackage -AllUsers -Name \"{appName}\";" +
                            "if ($package) {" +
                                $"Write-Host \"Removing app {appName}...\";" +
                                "$package | Remove-AppxPackage -AllUsers;" +
                            "}" +
                            "else {" +
                                $"Write-Host \"App {appName} is not installed.\";" +
                            "}";

                        if (removalMode == UWPAppRemovalMode.RemoveProvisionedPackages)
                        {
                            appRemovalScript +=
                                "$provisionedPackage = Get-AppxProvisionedPackage -Online | where {$_.DisplayName -eq \"" + appName + "\"};" +
                                "if ($provisionedPackage) {" +
                                    $"Write-Host \"Removing provisioned package for app {appName}...\";" +
                                    "Remove-AppxProvisionedPackage -Online -PackageName $provisionedPackage.PackageName;" +
                                "}" +
                                "else {" +
                                    $"Write-Host \"No provisioned package found for app {appName}\";" +
                                "}";
                        }

                        psInstance.RunScriptAndPrintOutput(appRemovalScript);
                        Console.WriteLine();

                        if (!atLeastOneAppUninstalled)
                            atLeastOneAppUninstalled = psInstance.GetVariable("package").IsNotEmpty();
                    }

                    // We check also if at least one app has been uninstalled to avoid
                    // performing tasks when app is not installed anymore
                    if (atLeastOneAppUninstalled && psInstance.Streams.Error.Count == 0)
                    {
                        Console.WriteLine($"Performing post-uninstall operations for app {appGroup}...");
                        PerformPostUninstallOperations(appGroup);
                    }
                }
            }
        }

        /**
         * Removes any eventual services, scheduled tasks and/or registry keys related to the specified app group.
         * In certain cases this method is used to remove certain apps that can be removed only by using install-wim-tweak.
         */
        private void PerformPostUninstallOperations(UWPAppGroup appGroup)
        {
            if (postUninstallOperationsForGroup.ContainsKey(appGroup))
                postUninstallOperationsForGroup[appGroup]();
            else
                Console.WriteLine("Nothing to do.");
        }

        public static void RemoveEdgeResidualFiles()
        {
            Console.WriteLine("Removing old files...");
            SystemUtils.DeleteDirectoryIfExists(
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\MicrosoftEdgeBackups",
                handleErrors: true
            );
        }

        private static void RemoveMapsServicesAndTasks()
        {
            Console.WriteLine("Removing app-related services and scheduled tasks...");
            new ServiceRemover(new[] { "MapsBroker", "lfsvc" })
                .PerformBackup()
                .PerformRemoval();

            new ScheduledTasksDisabler(new[] {
                @"\Microsoft\Windows\Maps\MapsUpdateTask",
                @"\Microsoft\Windows\Maps\MapsToastTask"
            }).PerformTask();
        }

        private static void RemoveXboxServicesAndTasks()
        {
            Console.WriteLine("Removing app-related services and scheduled tasks...");
            new ServiceRemover(new[] { "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc", "xbgm" })
                .PerformBackup()
                .PerformRemoval();

            new ScheduledTasksDisabler(new[] {
                @"Microsoft\XblGameSave\XblGameSaveTask",
                @"Microsoft\XblGameSave\XblGameSaveTaskLogon"
            }).PerformTask();

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
                key.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
        }

        private static void RemoveMessagingService()
        {
            Console.WriteLine("Removing app-related services...");
            new ServiceRemover(new[] { "MessagingService" })
                .PerformBackup()
                .PerformRemoval();
        }

        private static void RemovePaint3DContextMenuEntries()
        {
            Console.WriteLine("Removing Paint 3D context menu entries...");
            ShellUtils.ExecuteWindowsCommand(@"echo off & for /f ""tokens=1* delims="" %I in " +
                                              @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Edit"" ^| find /i ""3D Edit"" ') " +
                                             @"do (reg delete ""%I"" /f )");
        }

        private static void RemovePrint3DContextMenuEntries()
        {
            Console.WriteLine("Removing 3D Print context menu entries...");
            ShellUtils.ExecuteWindowsCommand(@"echo off & for /f ""tokens=1* delims="" %I in " +
                                              @"(' reg query ""HKEY_CLASSES_ROOT\SystemFileAssociations"" /s /k /f ""3D Print"" ^| find /i ""3D Print"" ') " +
                                             @"do (reg delete ""%I"" /f )");
        }

        private static void RemoveMailAndPeopleService()
        {
            Console.WriteLine("Removing app-related services...");
            new ServiceRemover(new[] { "OneSyncSvc" })
                .PerformBackup()
                .PerformRemoval();
        }

        private static void RemoveStoreFeaturesAndServices()
        {
            OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-ContentDeliveryManager");
            OperationUtils.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-Store");

            Console.WriteLine("Writing values into the Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"Software\Policies\Microsoft\WindowsStore"))
            {
                key.SetValue("RemoveWindowsStore", 1, RegistryValueKind.DWord);
                key.SetValue("DisableStoreApps", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\PushToInstall"))
                key.SetValue("DisablePushToInstall", 1, RegistryValueKind.DWord);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                key.SetValue("SilentInstalledAppsEnabled", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost"))
                key.SetValue("EnableWebContentEvaluation", 0, RegistryValueKind.DWord);

            Console.WriteLine("Removing app-related services...");
            new ServiceRemover(new[] { "PushToInstall" })
                .PerformBackup()
                .PerformRemoval();
        }
    }
}
