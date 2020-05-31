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

        private readonly IUserInterface ui;
        public TelemetryDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            RemoveTelemetryServices();
            DisableTelemetryFeatures();
        }

        private void RemoveTelemetryServices()
        {
            ui.PrintHeading("Backing up and removing telemetry services...");
            ServiceRemover.BackupAndRemove(telemetryServices, ui);

            new ServiceRemover(protectedTelemetryServices, ui).PerformBackup();
            RemoveProtectedServices();
            ui.PrintEmptySpace();
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
         *  Additional tasks to disable telemetry-related features.
         *  They include blocking of CompatTelRunner, DeviceCensus, Inventory (collection of installed programs),
         *   SmartScreen, Steps Recorder, Compatibility Assistant
         */
        private void DisableTelemetryFeatures()
        {
            ui.PrintHeading("Performing some registry edits to disable telemetry-related features...");

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener"))
                key.SetValue("Start", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
            {
                key.SetValue("AITEnable", 0, RegistryValueKind.DWord);
                key.SetValue("DisableInventory", 1, RegistryValueKind.DWord);
                key.SetValue("DisablePCA", 1, RegistryValueKind.DWord);
                key.SetValue("DisableUAR", 1, RegistryValueKind.DWord);
            }
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System"))
                key.SetValue("EnableSmartScreen", 0, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\" +
                   @"Image File Execution Options\CompatTelRunner.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\" +
                   @"Image File Execution Options\DeviceCensus.exe"))
                key.SetValue("Debugger", @"%windir%\System32\taskkill.exe", RegistryValueKind.String);
        }
    }
}
