using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        private static readonly string[] PROTECTED_TELEMETRY_SERVICES = new[] {
            "DPS",
            "WdiSystemHost",
            "WdiServiceHost"
        };

        public void PerformTask()
        {
            RemoveTelemetryServices();
            ConsoleUtils.WriteLine("Performing some registry edits to disable telemetry-related features...", ConsoleColor.Green);
            DisableTelemetryFeaturesViaRegistryEdits();
        }

        private void RemoveTelemetryServices()
        {
            ConsoleUtils.WriteLine("Backing up and removing telemetry services...", ConsoleColor.Green);
            new ServiceRemover(TELEMETRY_SERVICES)
                .PerformBackup()
                .PerformRemoval();

            new ServiceRemover(PROTECTED_TELEMETRY_SERVICES).PerformBackup();
            RemoveProtectedServices();
        }

        private void RemoveProtectedServices()
        {
            using (RegistryKey allServicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", true))
            {
                foreach (string service in PROTECTED_TELEMETRY_SERVICES)
                {
                    try
                    {
                        allServicesKey.GrantFullControlOnSubKey(service);

                        using (RegistryKey serviceKey = allServicesKey.OpenSubKey(service, true))
                        {
                            foreach (string subkeyName in serviceKey.GetSubKeyNames())
                                serviceKey.GrantFullControlOnSubKey(subkeyName);
                        }

                        allServicesKey.DeleteSubKeyTree(service);
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"Service {service} is not present or its key can't be retrieved.");
                    }
                    catch (Exception exc)
                    {
                        ConsoleUtils.WriteLine($"Error while trying to delete service {service}: {exc.Message}", ConsoleColor.Red);
                    }
                }
            }
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
