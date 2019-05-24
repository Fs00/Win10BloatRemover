using Microsoft.Win32;
using System;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class TelemetryDisabler : IOperation
    {
        private static readonly string[] TELEMETRY_SERVICES = new[] {
            "DiagTrack",
            "diagsvc",
            "diagnosticshub.standardcollector.service",
            "PcaSvc"
        };

        public void PerformTask()
        {
            RemoveTelemetryServices();
            ConsoleUtils.WriteLine("Performing some registry edits to disable telemetry-related features...", ConsoleColor.Green);
            DisableTelemetryFeaturesViaRegistryEdits();
            ConsoleUtils.WriteLine("You may also want to remove DPS, WdiSystemHost and WdiServiceHost services, " +
                                   "which can't be easily deleted programmatically due to their permissions.\n" +
                                   "Follow this steps to do it: github.com/adolfintel/Windows10-Privacy/blob/master/data/delkey.gif", ConsoleColor.Cyan);
        }

        private void RemoveTelemetryServices()
        {
            ConsoleUtils.WriteLine("Backing up and removing telemetry services...", ConsoleColor.Green);
            new ServiceRemover(TELEMETRY_SERVICES)
                .PerformBackup()
                .PerformRemoval();
        }

        /**
         *  Additional tasks to disable telemetry-related features
         *  Include blocking of CompatTelRunner, DeviceCensus, Inventory (collection of installed programs),
         *   SmartScreen, Steps Recorder, Compatibility Assistant
         */
        private void DisableTelemetryFeaturesViaRegistryEdits()
        {
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
