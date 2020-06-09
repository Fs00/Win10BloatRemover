using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
            @"\Microsoft\Windows\Application Experience\StartupAppTask",
            @"\Microsoft\Windows\Autochk\Proxy",
            @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
            @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
            @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
            @"\Microsoft\Windows\Device Information\Device",
            @"\Microsoft\Windows\NetTrace\GatherNetworkInfo",
            @"\Microsoft\Windows\PI\Sqm-Tasks"
        };

        private readonly IUserInterface ui;
        public TelemetryDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            RemoveTelemetryServices();
            DisableTelemetryFeatures();
            DisableTelemetryScheduledTasks();
        }

        private void RemoveTelemetryServices()
        {
            ui.PrintHeading("Backing up and removing telemetry services...");
            ServiceRemover.BackupAndRemove(telemetryServices, ui);

            new ServiceRemover(protectedTelemetryServices, ui).PerformBackup();
            RemoveProtectedServices();
        }

        private void RemoveProtectedServices()
        {
            using (TokenPrivilege.TakeOwnership)
            {
                using RegistryKey allServicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", writable: true);
                foreach (string serviceName in protectedTelemetryServices)
                    TryRemoveProtectedService(serviceName, allServicesKey);
            }
        }

        private void TryRemoveProtectedService(string serviceName, RegistryKey allServicesKey)
        {
            try
            {
                RemoveProtectedService(serviceName, allServicesKey);
                ui.PrintMessage($"Service {serviceName} removed successfully.");
            }
            catch (KeyNotFoundException)
            {
                ui.PrintMessage($"Service {serviceName} is not present or its key can't be retrieved.");
            }
            catch (Exception exc)
            {
                ui.PrintError($"Error while trying to delete service {serviceName}: {exc.Message}");
            }
        }

        private void RemoveProtectedService(string serviceName, RegistryKey allServicesKey)
        {
            allServicesKey.GrantFullControlOnSubKey(serviceName);
            using RegistryKey serviceKey = allServicesKey.OpenSubKey(serviceName, writable: true);
            foreach (string subkeyName in serviceKey.GetSubKeyNames())
            {
                // Protected subkeys must first be opened only with TakeOwnership right,
                // otherwise the system would prevent us to access them to edit their ACL.
                serviceKey.TakeOwnershipOnSubKey(subkeyName);
                serviceKey.GrantFullControlOnSubKey(subkeyName);
            }
            allServicesKey.DeleteSubKeyTree(serviceName);
        }

        /*
         *  Disabled telemetry-related features include CompatTelRunner, DeviceCensus,
         *  Inventory (collection of installed programs), SmartScreen, Steps Recorder, Compatibility Assistant
         */
        private void DisableTelemetryFeatures()
        {
            ui.PrintHeading("Performing some registry edits to disable telemetry features...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
            {
                key.SetValue("AITEnable", 0);
                key.SetValue("DisableInventory", 1);
                key.SetValue("DisablePCA", 1);
                key.SetValue("DisableUAR", 1);
            }
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener", "Start", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0);
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                "NoGenTicket", 1
            );
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe",
                "Debugger", @"%windir%\System32\taskkill.exe",
                RegistryValueKind.ExpandString
            );
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe",
                "Debugger", @"%windir%\System32\taskkill.exe",
                RegistryValueKind.ExpandString
            );
        }

        private void DisableTelemetryScheduledTasks()
        {
            ui.PrintHeading("Disabling telemetry-related scheduled tasks...");
            new ScheduledTasksDisabler(telemetryScheduledTasks, ui).Run();
        }
    }
}
