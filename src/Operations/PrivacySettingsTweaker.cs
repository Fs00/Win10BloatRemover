using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class PrivacySettingsTweaker : IOperation
    {
        private readonly IUserInterface ui;
        public PrivacySettingsTweaker(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            ui.PrintMessage("Writing values into the Registry...");
            AdjustPrivacySettings();
            DisableSensitiveDataSynchronization();
            DenySensitivePermissionsToApps();
            DisableWebAndLocationAccessToSearch();
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
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0);
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);

            // Privacy -> Inking and typing personalization (and related policies)
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0);
            Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\TabletPC", "PreventHandwritingDataSharing", 1);

            // Privacy -> Diagnostics and feedback -> Improve inking and typing recognition
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
                "AllowLinguisticDataCollection", 0
            );

            // Privacy -> Speech
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization", "AllowInputPersonalization", 0);
        }

        private void DisableSensitiveDataSynchronization()
        {
            // Privacy -> Activity history -> Send my activity history to Microsoft
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0);

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging", "AllowMessageSync", 0);
        }

        private void DenySensitivePermissionsToApps()
        {
            string[] permissionsToDeny = { "location", "documentsLibrary", "userDataTasks", "appDiagnostics", "userAccountInformation" };
            foreach (string permission in permissionsToDeny)
            {
                Registry.SetValue(
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{permission}",
                    "Value", "Deny"
                );
            }
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", 2);
        }

        private void DisableWebAndLocationAccessToSearch()
        {
            using RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
            key.SetValue("AllowSearchToUseLocation", 0);
            key.SetValue("DisableWebSearch", 1);
            key.SetValue("ConnectedSearchUseWeb", 0);
        }
    }
}
