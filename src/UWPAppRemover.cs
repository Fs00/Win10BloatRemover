using Microsoft.Win32;
using System;
using System.Management.Automation;

namespace Win10BloatRemover
{
    public enum UWPAppGroup
    {
        Bing,               // Weather, Finance, News etc.
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
        MixedReality,       // includes Paint 3D
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

        public UWPAppRemover(UWPAppGroup[] appsToRemove)
        {
            this.appsToRemove = appsToRemove;
        }

        public void PerformRemoval()
        {
            if (removalPerformed)
                throw new InvalidOperationException("Apps have been already removed!");

            PowerShell psInstance = PowerShell.Create();
            foreach (UWPAppGroup appGroup in appsToRemove)
            {
                foreach (string appName in GetAppNamesForGroup(appGroup))
                {
                    // The following script uninstalls the specified app package for all users when it is found
                    // and removes the package from Windows image (so that new users don't find the removed app)
                    string appRemovalScript = $"$package = Get-AppxPackage -AllUsers -Name \"{appName}\";" +
                                               "if ($package) { Remove-AppxPackage -AllUsers $package;" +
                                               "$provisionedPackage = Get-AppxProvisionedPackage -Online | where {$_.DisplayName -eq \"$package.Name\"};" +
                                               "if ($provisionedPackage) { Remove-AppxProvisionedPackage -Online -PackageName $provisionedPackage.PackageName; } }";

                    Console.WriteLine($"Removing {appName} app...");
                    psInstance.RunScriptAndPrintOutput(appRemovalScript);
                }

                // Perform post-uninstall operations only if package removal was successful
                if (!psInstance.HadErrors)
                {
                    Console.WriteLine($"Performing post-uninstall operations for app {appGroup}...");
                    PerformPostUninstallOperations(appGroup);
                }
                else
                {
                    // This is a workaround to avoid previous errors being rewritten to the error stream
                    // every time a script is executed (which is supposedly a PowerShell API bug)
                    psInstance.Dispose();
                    psInstance = PowerShell.Create();
                }
            }
            psInstance.Dispose();
            removalPerformed = true;
        }

        private void PerformPostUninstallOperations(UWPAppGroup appGroup)
        {
            switch (appGroup)
            {
                case UWPAppGroup.Mobile:
                    Operations.RemoveComponentUsingInstallWimTweak("Microsoft-PPIProjection-Package");
                    break;
                case UWPAppGroup.HelpAndFeedback:
                    Operations.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-ContactSupport");
                    break;
                case UWPAppGroup.Maps:
                    var serviceRemover = new ServiceRemover(new[] { "MapsBroker", "lfsvc" });
                    serviceRemover.PerformBackup();
                    serviceRemover.PerformRemoval();
                    SystemUtils.ExecuteWindowsCommand("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /disable");
                    break;
                case UWPAppGroup.Messaging:
                    var svcRemover = new ServiceRemover(new[] { "MessagingService" });
                    svcRemover.PerformBackup();
                    svcRemover.PerformRemoval();
                    break;
                case UWPAppGroup.MailAndCalendar:
                case UWPAppGroup.People:
                    // TODO ADD WARNING
                    var svcRemover2 = new ServiceRemover(new[] { "OneSyncSvc" });
                    svcRemover2.PerformBackup();
                    svcRemover2.PerformRemoval();
                    break;
                case UWPAppGroup.Xbox:
                    var serviceRemover2 = new ServiceRemover(new[] { "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc", "xbgm" });
                    serviceRemover2.PerformBackup();
                    serviceRemover2.PerformRemoval();
                    SystemUtils.ExecuteWindowsCommand("schtasks /Change /TN \"Microsoft\\XblGameSave\\XblGameSaveTask\" /disable & " +
                                                      "schtasks /Change /TN \"Microsoft\\XblGameSave\\XblGameSaveTaskLogon\" /disable");
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
                        key.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
                    break;
                default:
                    Console.WriteLine("Nothing to do.");
                    break;
            }
        }

        private static string[] GetAppNamesForGroup(UWPAppGroup appGroup)
        {
            switch (appGroup)
            {
                case UWPAppGroup.Bing:
                    return new[] {
                        "Microsoft.BingNews",
                        "Microsoft.BingWeather"
                    };
                case UWPAppGroup.AlarmsAndClock:
                    return new[] { "Microsoft.WindowsAlarms" };
                case UWPAppGroup.Calculator:
                    return new[] { "Microsoft.WindowsCalculator" };
                case UWPAppGroup.Camera:
                    return new[] { "Microsoft.WindowsCamera" };
                case UWPAppGroup.HelpAndFeedback:
                    return new[] {
                        "Microsoft.WindowsFeedbackHub",
                        "Microsoft.GetHelp",
                        "Microsoft.Getstarted"
                    };
                case UWPAppGroup.MailAndCalendar:
                    return new[] { "microsoft.windowscommunicationsapps" };
                case UWPAppGroup.Maps:
                    return new[] { "Microsoft.WindowsMaps" };
                case UWPAppGroup.Messaging:
                    return new[] { "Microsoft.Messaging" };
                case UWPAppGroup.MixedReality:         // unsure if it will remain
                    return new[] {
                        "Microsoft.Microsoft3DViewer",
                        "Microsoft.MSPaint",
                        "Microsoft.Print3D",
                        "Microsoft.MixedReality.Portal"
                    };
                case UWPAppGroup.Mobile:
                    return new[] {
                        "Microsoft.YourPhone",
                        "Microsoft.OneConnect"
                    };
                case UWPAppGroup.OfficeHub:
                    return new[] { "Microsoft.MicrosoftOfficeHub" };
                case UWPAppGroup.OneNote:
                    return new[] { "Microsoft.Office.OneNote" };
                case UWPAppGroup.People:
                    return new[] { "Microsoft.People" };
                case UWPAppGroup.Photos:
                    return new[] { "Microsoft.Windows.Photos" };
                case UWPAppGroup.Skype:
                    return new[] { "Microsoft.SkypeApp" };
                case UWPAppGroup.SnipAndSketch:
                    return new[] { "Microsoft.SkreenSketch" };
                case UWPAppGroup.SolitaireCollection:
                    return new[] { "Microsoft.MicrosoftSolitaireCollection" };
                case UWPAppGroup.StickyNotes:
                    return new[] { "Microsoft.MicrosoftStickyNotes" };
                case UWPAppGroup.Xbox:
                    return new[] {
                        "Microsoft.XboxGameCallableUI",
                        "Microsoft.XboxSpeechToTextOverlay",
                        "Microsoft.XboxApp",
                        "Microsoft.XboxGameOverlay",
                        "Microsoft.XboxGamingOverlay",
                        "Microsoft.XboxIdentityProvider",
                        "Microsoft.Xbox.TCUI"
                    };
                case UWPAppGroup.Zune:
                    return new[] {
                        "Microsoft.ZuneMusic",
                        "Microsoft.ZuneVideo"
                    };
                default:
                    return new string[0];
            }
        }
    }
}
