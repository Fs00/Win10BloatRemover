using Microsoft.Win32;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations;

public class PrivacySettingsTweaker(IUserInterface ui) : IOperation
{
    private static readonly string[] appPermissionsToDeny = [
        "location",
        "documentsLibrary",
        "userDataTasks",
        "appDiagnostics",
        "userAccountInformation"
    ];

    public void Run()
    {
        ui.PrintMessage("Writing values into the Registry...");
        AdjustPrivacySettings();
        DisableSensitiveDataSynchronization();
        DenySensitivePermissionsToApps();
        DenyLocationAccessToSearch();
    }

    private void AdjustPrivacySettings()
    {
        // Account -> Sign-in options -> Privacy
        Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "DisableAutomaticRestartSignOn", 1
        );

        // Privacy -> General
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1);
        RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0);
        RegistryUtils.SetForCurrentAndDefaultUser(@"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);

        // Privacy -> Inking and typing personalization (and related policies)
        RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0);
        RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\TabletPC", "PreventHandwritingDataSharing", 1);

        // Privacy -> Diagnostics and feedback -> Improve inking and typing recognition
        Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
            "AllowLinguisticDataCollection", 0);

        // Privacy -> Speech
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization", 0);

        // Microsoft Edge settings -> Privacy, search and services -> Personalize your web experience
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "PersonalizationReportingEnabled", 0);
    }

    private void DisableSensitiveDataSynchronization()
    {
        // Privacy -> Activity history -> Send my activity history to Microsoft
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0);

        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging", "AllowMessageSync", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSync", 2);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\SettingSync", "DisableCredentialsSettingSyncUserOverride", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\SettingSync", "DisableApplicationSettingSync", 2);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\SettingSync", "DisableApplicationSettingSyncUserOverride", 1);
    }

    private void DenySensitivePermissionsToApps()
    {
        foreach (string permission in appPermissionsToDeny)
        {
            using var permissionKey = RegistryUtils.LocalMachine64.CreateSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{permission}"
            );
            permissionKey.SetValue("Value", "Deny");
        }
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", 2);
    }

    private void DenyLocationAccessToSearch()
    {
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowSearchToUseLocation", 0);
    }
}
