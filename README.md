# Windows 10 Bloat Remover and Tweaker
This configurable tool provides an interactive command-line interface to aggressively debloat and tweak your Windows 10 installation in an easy way. Here's what it can do for you:

* **Make system apps removable** by editing an internal system database. Thanks to this, apps like Edge, Security Center and others can be uninstalled using default PowerShell commands. Take note that usually system apps get reinstalled by Windows cumulative updates.  
* **Remove pre-installed UWP apps:** Uninstalls the apps specified by the user either for the current Windows user or for all users (see *Configuration* below, options `UWPAppsToRemove` and `UWPAppsRemovalMode`). When apps are uninstalled for all users, their corresponding provisioned packages are deleted too (if present), so that they won't get reinstalled for new users or after feature updates.  
* **Disable OS telemetry:** disables several Windows components that collect diagnostic and usage information such as Compatibility Telemetry, Inventory, Device Census, Customer Experience Improvement Program and others. It also deletes the services which are responsible for data reporting to Microsoft.
* **Remove system services:** deletes - not just disables - the services specified by the user (see *Configuration* below, option `ServicesToRemove`) after backing up their Registry keys, so that you can restore them if anything breaks.
* **Tweak Windows settings for enhanced privacy:** makes Windows more privacy-respectful by turning off certain system features that put your personal data at risk, such as inking/typing personalization, app launch tracking, clipboard/text messages synchronization, voice activation and some more. Take note that the goal here is to provide a mindful balance that leans towards privacy, without sacrificing too much in terms of user experience.
* **Disable/remove Windows Defender:** the user can choose either to just disable it or to fully eradicate it from the system (see *Configuration* below, option `AllowInstallWimTweak`). If you make system apps removable, Windows Security app will be deleted too.
* **Remove OneDrive** using the uninstaller provided by Microsoft, its folder in Explorer sidebar will also be hidden. Furthermore, its automatic setup will be disabled to prevent the app from being installed for new users.
* **Remove Windows feature packages:** uninstalls the [Feature-On-Demand (FOD) packages](https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/features-on-demand-v2--capabilities) specified by the user (see *Configuration* below, option `WindowsFeaturesToRemove`).
* **Disable automatic updates:** prevents automatic download and installing of Windows and Store apps updates through Group Policies. Take note that **automatic Windows Updates can't be disabled on Windows 10 Home.**
* **Disable Cortana:** accomplished using Group Policies until OS version 1909. Starting from version 2004, it can be removed like any other pre-installed app.
* **Disable Windows Error Reporting (WER)**: thanks to this, Windows will no longer "check for a solution to the problem" when a program crashes.
* **Disable Tips, Spotlight and feedback requests** through Group Policies
* **Disable scheduled tasks** specified by the user (see *Configuration* below, option `ScheduledTasksToDisable`)

Be aware that while most of these operations can be reverted with a system restore point, **some of them cannot** (uninstalling FODs/provisioned app packages/Windows Defender), and carry over after major Windows updates and full system restores.

## Configuration
Program settings are stored in [JSON format](https://en.wikipedia.org/wiki/JSON) in a file called *config.json*, located in the same folder as the program's executable. If said file is not found (e.g. when launching the tool for the first time), it is created containing the default settings.

If the program isn't able to load the configuration from the file for some reason, the error will be displayed when the application starts up and options will be populated with their default values.

Inside the settings file, you will find six options described below:

### `UWPAppsToRemove`
Configures which pre-installed UWP apps should be uninstalled. Take note that you can't choose to remove single UWP packages but only groups of them, to make configuration less tricky and also because some apps are made of multiple packages (e.g. Xbox) which depend on common services or components that get removed by the program.

**Allowed values:** an array which can contain the following values (each one represents a group of apps - a group can consist in a single app):
* `"Edge"` (you need to make system apps removable to uninstall it)
* `"Bing"` (Weather, News, Finance and Sports)
* `"Mobile"` (Your Phone and Mobile plans)
* `"Xbox"` (Xbox app, Game Overlay and related services)
* `"OfficeHub"` (My Office)
* `"OneNote"`
* `"Camera"`
* `"Cortana"` (on Windows version 2004 or higher)
* `"HelpAndFeedback"` (Feedback Hub, Get Help and Microsoft Tips)
* `"Maps"`
* `"Zune"` (Groove Music and Movies)
* `"CommunicationsApps"` (Mail, Calendar and People)
* `"Messaging"`
* `"SolitaireCollection"`
* `"StickyNotes"`
* `"MixedReality"` (3D Viewer, Print 3D and Mixed Reality Portal)
* `"Paint3D"`
* `"Skype"`
* `"Photos"` (after removal, legacy Photo Viewer will be restored for your convenience)
* `"AlarmsAndClock"`
* `"Calculator"`
* `"SnipAndSketch"`
* `"Store"`
* `"SoundRecorder"`

**Default value:** an array containing some of the app groups listed above

### `UWPAppsRemovalMode`
Configures whether to remove UWP apps for all present and future users (which is the default) or just for the current user. Take note that trying to remove system apps only for the current user might not always work.

**Allowed values:** `"AllUsers"` or `"CurrentUser"`  
**Default value:** `"AllUsers"`

### `ServicesToRemove`
Configures which system services should be removed by specifying their names.
Take note that for each name you specify, the program will remove the services *whose name starts with the specified name*. This is made in order to include those services whose name ends with a random code.

**Allowed values:** an array containing an arbitrary number of service names  
**Default value:** an array containing a set of services that are deemed superfluous or undesirable for expert users:
* `"dmwappushservice"`
* `"RetailDemo"`
* `"TroubleshootingSvc"` (runs automatic troubleshooters periodically)

### `ScheduledTasksToDisable`
Configures which scheduled tasks should be disabled by specifying their path. You can find the path of each scheduled task in the system with the following PowerShell command: `Get-ScheduledTask | foreach { $_.TaskPath + $_.TaskName }`.

**Allowed values:** an array containing an arbitrary number of scheduled tasks  
**Default value:** an array containing a set of scheduled tasks that are deemed superfluous or undesirable. [See the full list here](https://github.com/Fs00/Win10BloatRemover/blob/master/src/Configuration.cs#L106L120).

### `WindowsFeaturesToRemove`
Configures which Feature-On-Demand (FOD) packages should be removed by specifying their names. As with system services, the program will remove all packages whose name starts with the names you specify (particularly useful since often FODs are made up of multiple packages with very similar names).
You can find the names of all removable FOD packages on your system with the PowerShell command `(Get-WindowsPackage -Online).PackageName`.

**Allowed values:** an array containing an arbitrary number of FOD package names  
**Default value:** an array containing a set of FOD packages that are deemed superfluous for most users:
  - `"Microsoft-Windows-InternetExplorer"` (Internet Explorer 11)
  - `"Microsoft-Windows-Hello-Face"` (Windows Hello face authentication)
  - `"Microsoft-Windows-QuickAssist"` (Quick Assist app)
  - `"Microsoft-Windows-TabletPCMath"` (Math Input Panel, Control and Recognizer)
  - `"Microsoft-Windows-StepsRecorder"` (Steps Recorder)
  - `"Microsoft-Windows-WirelessDisplay"` (Connect app, pre-installed only on some devices)

### `AllowInstallWimTweak`
Configures whether hidden system FOD packages should be removed using install-wim-tweak, an open-source tool which comes bundled together with the program. This tool is used only to fully remove Windows Defender and Connect app (the latter only for Windows versions prior to 2004).  
*A bit of background:* Despite the tool being safe, we noticed that removing certain system FODs caused the inability to install Windows cumulative updates (error 0x800f081f). We identified those critical FODs and changed the program to avoid removing them, but since we cannot guarantee that similar issues won't come up again in the future, we added the ability to choose between a more aggressive and a more cautious approach.

**Allowed values:** `true` or `false`  
**Default value:** `false`

## Release cycle and versions
The binaries of this tool can be used only on a specific Windows version, so that, for example, you have an EXE for version 1809 and one for 1903, with the latter raising an error when run on an OS version different from 1903. This is done to ease developer maintenance and to make the tool impossible to run on an incompatible system (mistake-proof😉).  
The third segment of the program version is the supported Windows version (it was the first segment in the first releases of the tool), so you can see at a glance if you have the right binary for your system.  
The tool will be updated after any new Windows version. Only the latest two versions of Windows will be supported at the same time. The master branch will host the source code for the most recent version of Windows.

## Credits
This tool was originally based on Federico Dossena's [Windows 10 de-botnet guide](https://github.com/adolfintel/Windows10-Privacy), which is now discontinued.  
Over time, the program evolved on its own, taking sometimes inspiration from the work made by other open source developers:
  - [**privacy.sexy** website](https://github.com/undergroundwires/privacy.sexy) by @undergroundwires
  - [**Debloat Windows 10** scripts](https://github.com/W4RH4WK/Debloat-Windows-10) by @W4RH4WK

## Download
Head to Releases.
