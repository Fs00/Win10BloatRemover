using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Win10BloatRemover
{
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
        SnipAndSketch
    }

    /**
     *  UWPAppRemover
     *  Removes the UWP which are passed into the constructor
     *  Once removal is performed, the class instance can not be reused
     */
    class UWPAppRemover
    {
        private readonly UWPAppGroup[] appsToRemove;
        private bool removalPerformed = false;

        // This dictionary contains the exact apps names corresponding to every group in the enum
        private static readonly Dictionary<UWPAppGroup, string[]> appNamesForGroup = new Dictionary<UWPAppGroup, string[]> {
            { UWPAppGroup.AlarmsAndClock, new[] { "Microsoft.WindowsAlarms" } },
            { UWPAppGroup.Bing, new[] { "Microsoft.BingNews", "Microsoft.BingWeather" } },
            { UWPAppGroup.Calculator, new[] { "Microsoft.WindowsCalculator" } },
            { UWPAppGroup.Camera, new[] { "Microsoft.WindowsCamera" } },
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

        public UWPAppRemover(UWPAppGroup[] appsToRemove)
        {
            this.appsToRemove = appsToRemove;
        }

        public void PerformRemoval()
        {
            if (removalPerformed)
                throw new InvalidOperationException("Apps have been already removed!");

            using (PowerShell psInstance = PowerShell.Create())
            {
                foreach (UWPAppGroup appGroup in appsToRemove)
                {
                    bool appUninstalled = false;
                    foreach (string appName in appNamesForGroup[appGroup])
                    {
                        // The following script uninstalls the specified app package for all users when it is found
                        // and removes the package from Windows image (so that new users don't find the removed app)
                        string appRemovalScript =
                            $"$package = Get-AppxPackage -AllUsers -Name \"{appName}\";" +
                            "if ($package) {" +
                                $"Write-Host \"Removing app $($package.Name)...\";" +
                                "Remove-AppxPackage -AllUsers $package;" +
                                "$provisionedPackage = Get-AppxProvisionedPackage -Online | where {$_.DisplayName -eq $package.Name};" +
                                "if ($provisionedPackage) {" +
                                    "Write-Host \"Removing provisioned package for app $($package.Name)...\";" +
                                    "Remove-AppxProvisionedPackage -Online -PackageName $provisionedPackage.PackageName;" +
                                "}" +
                                "else { Write-Host \"No provisioned package found for app $($package.Name)\"; }" +
                            "}" +
                            "else {" +
                                $"Write-Host \"App {appName} is not installed.\";" +
                            "}";

                        ConsoleUtils.WriteLine($"\nRemoving {appName} app...", ConsoleColor.Green);
                        psInstance.RunScriptAndPrintOutput(appRemovalScript);

                        // Check if uninstall has been performed for at least one app of the group
                        if (!appUninstalled)
                            appUninstalled = psInstance.Runspace.SessionStateProxy.PSVariable.Get("package").Value.ToString() != "";
                    }

                    // Perform post-uninstall operations only if package removal was successful and at least one
                    // app of the group has been uninstalled (avoids performing the tasks when app is not installed anymore)
                    if (appUninstalled && psInstance.Streams.Error.Count == 0)
                    {
                        Console.WriteLine($"Performing post-uninstall operations for app {appGroup}...");
                        PerformPostUninstallOperations(appGroup);
                    }
                }
            }
            removalPerformed = true;
        }

        /**
         * Removes any eventual services, scheduled tasks and/or registry keys related to the specified app group.
         * In certain cases this method is used to remove apps that can be removed only by using install-wim-tweak.
         */
        private void PerformPostUninstallOperations(UWPAppGroup appGroup)
        {
            switch (appGroup)
            {
                case UWPAppGroup.Mobile:
                    Operations.RemoveComponentUsingInstallWimTweak("Microsoft-PPIProjection-Package");  // Connect app
                    break;

                case UWPAppGroup.HelpAndFeedback:
                    Operations.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-ContactSupport");
                    break;

                case UWPAppGroup.Maps:
                    Console.WriteLine("Removing app-related services...");
                    new ServiceRemover(new[] { "MapsBroker", "lfsvc" }).PerformBackup().PerformRemoval();
                    SystemUtils.ExecuteWindowsCommand(@"schtasks /Change /TN ""\Microsoft\Windows\Maps\MapsUpdateTask"" /disable");
                    break;

                case UWPAppGroup.Messaging:
                    Console.WriteLine("Removing app-related services...");
                    new ServiceRemover(new[] { "MessagingService" }).PerformBackup().PerformRemoval();
                    break;

                case UWPAppGroup.MailAndCalendar:
                case UWPAppGroup.People:
                    Console.WriteLine("Removing app-related services...");
                    new ServiceRemover(new[] { "OneSyncSvc" }).PerformBackup().PerformRemoval();
                    break;

                case UWPAppGroup.Xbox:
                    Console.WriteLine("Removing app-related services...");
                    new ServiceRemover(new[] { "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc", "xbgm" })
                        .PerformBackup()
                        .PerformRemoval();
                    SystemUtils.ExecuteWindowsCommand(@"schtasks /Change /TN ""Microsoft\XblGameSave\XblGameSaveTask"" /disable & " +
                                                      @"schtasks /Change /TN ""Microsoft\XblGameSave\XblGameSaveTaskLogon"" /disable");
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
                        key.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
                    break;

                default:
                    Console.WriteLine("Nothing to do.");
                    break;
            }
        }
    }
}
