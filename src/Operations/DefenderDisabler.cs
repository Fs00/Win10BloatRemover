using Microsoft.Win32;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class DefenderDisabler : IOperation
    {
        private static readonly string[] securityHealthServices = {
            "SecurityHealthService",
            "wscsvc",
            "Sense",
            "SgrmBroker",
            "SgrmAgent"
        };

        private readonly IUserInterface ui;
        private readonly IOperation securityCenterRemover;
        private readonly ServiceRemover serviceRemover;

        public bool IsRebootRecommended { get; private set; }

        public DefenderDisabler(IUserInterface ui, IOperation securityCenterRemover, ServiceRemover serviceRemover)
        {
            this.ui = ui;
            this.securityCenterRemover = securityCenterRemover;
            this.serviceRemover = serviceRemover;
        }

        public void Run()
        {
            DowngradeAntimalwarePlatform();
            EditWindowsRegistryKeys();
            RemoveSecurityHealthServices();
            securityCenterRemover.Run();
        }

        // DisableAntiSpyware policy is not honored anymore on Defender antimalware platform version 4.18.2007.8+
        // This workaround will last until Windows ships with a lower version of that platform pre-installed
        private void DowngradeAntimalwarePlatform()
        {
            ui.PrintHeading("Downgrading Defender antimalware platform...");
            int exitCode = SystemUtils.RunProcessBlockingWithOutput(
                $@"{SystemUtils.GetProgramFilesFolder()}\Windows Defender\MpCmdRun.exe", "-resetplatform", ui);

            if (exitCode != SystemUtils.EXIT_CODE_SUCCESS)
            {
                ui.PrintWarning(
                    "Antimalware platform downgrade failed. This is likely happened because you have already disabled Windows Defender.\n" +
                    "If this is not your case, you can proceed anyway but be aware that Defender will not be disabled fully " +
                    "if the antimalware platform has been updated to version 4.18.2007.8 or higher through Windows Update.");
                ui.ThrowIfUserDenies("Do you want to continue?");
            }
            IsRebootRecommended = true;
        }

        private void EditWindowsRegistryKeys()
        {
            ui.PrintHeading("Editing keys in Windows Registry...");

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0);
            RegistryUtils.SetForCurrentAndDefaultUser(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter", "EnabledV9", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet"))
            {
                key.SetValue("SpynetReporting", 0);
                key.SetValue("SubmitSamplesConsent", 2);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\MRT"))
            {
                key.SetValue("DontReportInfectionInformation", 1);
                key.SetValue("DontOfferThroughWUAU", 1);
            }

            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localMachine.DeleteSubKeyValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth");
            localMachine.DeleteSubKeyValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "SecurityHealth");

            using RegistryKey notificationSettings = localMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance"
            );
            notificationSettings.SetValue("Enabled", 0);
        }

        private void RemoveSecurityHealthServices()
        {
            ui.PrintHeading("Removing Security Health services...");
            serviceRemover.BackupAndRemove(securityHealthServices);
        }
    }
}
