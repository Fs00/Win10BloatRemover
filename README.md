# Windows 10 Bloat Remover and Tweaker
This configurable tool offers a simple CLI to aggressively debloat and apply some tweaks to your Windows 10 installation. Here's a comprehensive feature list:

* **Make system apps removable** by editing a system database. Thanks to this, apps like Edge, Connect and others can be deleted using normal PowerShell methods. Take note that usually system apps get reinstalled by Windows cumulative updates.  
* **UWP apps removal:** Uninstalls the apps specified in configuration file for all users and optionally deletes their corresponding provisioned packages (if present), so that apps aren't reinstalled for new users or after feature updates.  
Take note that you can't configure the program to remove single UWP packages but only groups of them, to make user configuration less tricky and because some apps are made of multiple packages (e.g. Xbox) which depend on each other. Groups are defined as follows:
    * ***Edge*** (you need to make system apps removable to uninstall it)
    * *Bing*: Weather, News, Finance and Sports
    * *Mobile*: YourPhone, Mobile plans and Connect app
    * *Xbox*: Xbox app, Game Overlay and related services
    * *OfficeHub*: My Office
    * *OneNote*
    * *Camera*
    * *HelpAndFeedback*: Feedback Hub, Get Help and Microsoft Tips
    * *Maps*
    * *Zune*: Groove Music and Movies
    * *CommunicationsApps*: Mail, Calendar and People (they all depend on a common service)
    * *Messaging*
    * *SolitaireCollection*
    * *StickyNotes*
    * *MixedReality*: 3D Viewer, Print 3D and Mixed Reality Portal
    * *Paint3D*
    * *Skype*
    * *Photos* (after removal, original Photo Viewer will be restored for your convenience)
    * *AlarmsAndClock*
    * *Calculator*
    * *SnipAndSketch*
    * *Store*
* **Automatic updates disabling:** Prevents automatic download and installing of Windows and Store apps updates through Group Policies. Therefore, this method won't work on Windows 10 Home.
* **Telemetry disabling:** disables several telemetry and features that collect user data such as Compatibility Telemetry, Inventory, Device Census, SmartScreen and others. It also deletes the services which are responsible for diagnostics and data reporting to Microsoft.
* **Services removal:** deletes (not just disables) the services specified in configuration after backing them up, so that you can restore them if any feature breaks.
* **Disabling and removal of Windows Defender:** the latter is accomplished using [install-wim-tweak](https://github.com/shiitake/win6x_registry_tweak) tool. If you make system apps removable, Windows Security app will be deleted too.
* **OneDrive removal** using stock uninstaller, its folder in Explorer sidebar will also be hidden. Furthermore, install-wim-tweak will be used to prevent the app to be re-installed for new users or after major Windows updates.
* **Windows features removal:** uninstalls the optional features packages specified in configuration. Take note that these features are the ones listed in Settings app, not the ones in Control Panel. You can find the names of feature packages that can be removed with the PowerShell command `Get-WindowsPackage -Online`.
* **Cortana disabling:** accomplished using Group Policies, since it is too deeply integrated with the system to be removed without consequences.
* **Windows Tips and Error Reporting disabling** through Registry edits
* **Scheduled tasks disabling** via schtasks.exe

**Most of these operations are NOT reversible**. If you don't know what you're doing, stay away from this tool.

## Configuration
When the program is run for the first time, a configuration file called *config.json* is created containing the default settings. You can easily edit which services, system features, scheduled tasks and groups of UWP apps (listed above) will be removed by adding or removing elements from the corresponding arrays (respectively `ServicesToRemove`, `WindowsFeaturesToRemove`, `ScheduledTasksToDisable` and `UWPAppsToRemove`).

Furthermore, you have two other options to reduce the aggressiveness of the debloating process:

* `UWPAppsRemovalMode`: allows you to decide not to remove provisioned packages by setting it to *KeepProvisionedPackages* (default is *RemoveProvisionedPackages*)
* `AllowInstallWimTweak`: can be set to *true* or *false* and if it's false, any execution of install-wim-tweak will be skipped (default is *false*)

These two settings have been added to make users able to limit the amount of changes made to the online Windows image, since they can't be reverted with system restore and carry over after major Windows updates.

If any parsing error occurs, it will be displayed at application start-up and default settings will be loaded.

## Release cycle and versions
The binaries of this tool are Windows version-specific (e.g. versions 1809 and 1903 have different executables) to avoid compatibility issues. The third segment of the program version is the supported Windows version (previously it was the first segment), so you can see at a glance if you have the right binary for your system.  
The tool will be updated whenever there is a new Windows version. The master branch will host the source code for the latest Windows version. Older versions up to one year will receive the latest features (if any) implemented in the master branch.

## Credits
Thanks to Federico Dossena for its [Windows 10 de-botnet guide](https://github.com/adolfintel/Windows10-Privacy), which this tool is based on.

## Download
Head to Releases.
