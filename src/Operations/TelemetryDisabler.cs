using Microsoft.Win32;
using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class TelemetryDisabler : IOperation
    {
        private static readonly string[] telemetryServices = {
            "DiagTrack",
            "diagsvc",
            "diagnosticshub.standardcollector.service",
            "PcaSvc"
        };

        private static readonly string[] protectedTelemetryServices = {
            "DPS",
            "WdiSystemHost",
            "WdiServiceHost"
        };

        private static readonly string[] telemetryScheduledTasks = {
            @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
            @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
            @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
            @"\Microsoft\Windows\Application Experience\StartupAppTask",
            @"\Microsoft\Windows\Autochk\Proxy",
            @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
            @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
            @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
            @"\Microsoft\Windows\Device Information\Device",
            @"\Microsoft\Windows\Device Information\Device User",
            @"\Microsoft\Windows\NetTrace\GatherNetworkInfo",
            @"\Microsoft\Windows\PI\Sqm-Tasks"
        };

        private readonly IUserInterface ui;
        private readonly ServiceRemover serviceRemover;

        public bool IsRebootRecommended { get; private set; }

        public TelemetryDisabler(IUserInterface ui, ServiceRemover serviceRemover)
        {
            this.ui = ui;
            this.serviceRemover = serviceRemover;
        }

        public void Run()
        {
            RemoveTelemetryServices();
            DisableTelemetryFeatures();
            DisableTelemetryScheduledTasks();
        }

        private void RemoveTelemetryServices()
        {
            ui.PrintHeading("Backing up and removing telemetry services...");
            serviceRemover.BackupAndRemove(telemetryServices);
            if (serviceRemover.IsRebootRecommended)
                IsRebootRecommended = true;

            string[] actualProtectedServices = serviceRemover.PerformBackup(protectedTelemetryServices);
            RemoveProtectedServices(actualProtectedServices);
        }

        private void RemoveProtectedServices(string[] protectedServicesToRemove)
        {
            using (TokenPrivilege.TakeOwnership)
            {
                using RegistryKey allServicesKey = Registry.LocalMachine.OpenSubKeyWritable(@"SYSTEM\CurrentControlSet\Services");
                foreach (string serviceName in protectedServicesToRemove)
                    TryRemoveProtectedService(serviceName, allServicesKey);
            }
        }

        private void TryRemoveProtectedService(string serviceName, RegistryKey allServicesKey)
        {
            try
            {
                RemoveProtectedService(serviceName, allServicesKey);
                ui.PrintMessage($"Service {serviceName} removed, but it will continue to run until the next restart.");
                IsRebootRecommended = true;
            }
            catch (Exception exc)
            {
                ui.PrintError($"Error while trying to delete service {serviceName}: {exc.Message}");
            }
        }

        private void RemoveProtectedService(string serviceName, RegistryKey allServicesKey)
        {
            allServicesKey.GrantFullControlOnSubKey(serviceName);
            using RegistryKey serviceKey = allServicesKey.OpenSubKeyWritable(serviceName);
            foreach (string subkeyName in serviceKey.GetSubKeyNames())
            {
                // Protected subkeys must first be opened only with TakeOwnership right,
                // otherwise the system would prevent us to access them to edit their ACL.
                serviceKey.TakeOwnershipOnSubKey(subkeyName);
                serviceKey.GrantFullControlOnSubKey(subkeyName);
            }
            allServicesKey.DeleteSubKeyTree(serviceName);
        }

        private void DisableTelemetryFeatures()
        {
            ui.PrintHeading("Performing some registry edits to disable telemetry features...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
            {
                key.SetValue("AITEnable", 0);   // Application Telemetry
                key.SetValue("DisableInventory", 1);
                key.SetValue("DisablePCA", 1);  // Program Compatibility Assistant
            }
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener", "Start", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\System", "AllowExperimentation", 0);
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                "NoGenTicket", 1
            );
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe",
                "Debugger", @"%windir%\System32\taskkill.exe"
            );
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe",
                "Debugger", @"%windir%\System32\taskkill.exe"
            );
        }

        private void DisableTelemetryScheduledTasks()
        {
            ui.PrintHeading("Disabling telemetry-related scheduled tasks...");
            new ScheduledTasksDisabler(telemetryScheduledTasks, ui).Run();
        }
    }
}
