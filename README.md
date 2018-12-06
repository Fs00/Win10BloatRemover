# Windows 10 Bloat Remover and Tweaker
This configurable tool offers a simple CLI to debloat and apply some tweaks to your Windows 10 installation. Here's a comprehensive feature list:

* **UWP apps removal**: Uninstalls the specified list of apps from all users and deletes their corresponding provisioned packages (if present), so that apps aren't reinstalled for new users or after system updates.  
Take note that you can't configure the program to remove single UWP packages but only groups of them, to make user configuration less tricky and because some apps are made of multiple packages (e.g. Xbox) which depend on each other. Groups are defined as follows:
	* *Bing*: Weather and News
    * *Mobile*: YourPhone, Mobile plans and Connect app
    * *Xbox*: Xbox app, Game Overlay and related services
    * *OfficeHub*: My Office
    * *OneNote*
    * *Camera*
    * *HelpAndFeedback*: Feedback Hub, Get Help and Microsoft Tips
    * *Maps*
    * *Zune*: Groove Music and Movies
    * *People*
    * *MailAndCalendar* (they belong to the same package)
    * *Messaging*
    * *SolitaireCollection*
    * *StickyNotes*
    * *MixedReality*: 3D Viewer, Print 3D and Mixed Reality Portal
    * *Paint3D*
    * *Skype*
    * *Photos*
    * *AlarmsAndClock*
    * *Calculator*
    * *SnipAndSketch*
* **Automatic updates disabling:** Prevents automatic download and installing of Windows and Store apps updates through Group Policies. Therefore, this method won't work on Windows 10 Home.
* **Services and telemetry removal:** deletes (not just disables) the services specified in configuration after backing them up, so that you can restore them if any feature breaks. In addition, it also disables several Microsoft telemetry features such as Compatibility Telemetry, Inventory and others.
* **Removal of Windows Defender and Microsoft Edge:** This is accomplished using [install-wim-tweak](https://github.com/shiitake/win6x_registry_tweak) tool. Please take note that Security Center UWP package won't be removed to avoid breaking the system.
* **OneDrive removal** using stock uninstaller, its folder in Explorer sidebar will also be hidden
* **Windows features removal:** uninstalls the optional features packages specified in configuration. Take note that these features are the ones listed in Settings app, not the ones in Control Panel. You can find the names of installed features packages with the PowerShell command `Get-WindowsPackage -Online`.
* **Cortana disabling:** accomplished using Group Policies, since removing it using install-wim-tweak would break Windows search.
* **Windows Tips and Error Reporting disabling** through Registry edits
* **Scheduled tasks disabling** via schtasks.exe

Warning: most of these operations are **NOT** reversible. If you don't know what you're doing, stay away from this tool.
**This tool supports only October 2018 Update.** Whenever a Windows major update is released, this tool will be updated too.

## Configuration
When the program is run for the first time, a configuration file called *config.json* is created containing the default settings. You can easily edit which services, system features, scheduled tasks and UWP app groups will be removed by adding or removing elements from the corresponding arrays. Please take note that `UWPAppsToRemove` array accepts only the values listed above.  
If any parsing error occurs, it will be displayed at application start-up and default settings will be loaded.

## Credits
Thanks to Federico Dossena for its [Windows 10 de-botnet guide](https://github.com/adolfintel/Windows10-Privacy), which this tool is based on.

## Download
Head to Releases.