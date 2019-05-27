using Microsoft.Win32;
using System;

namespace Win10BloatRemover.Operations
{
    class ErrorReportingDisabler : IOperation
    {
        private static readonly string[] ERROR_REPORTING_SERVICES = new[] { "WerSvc", "wercplsupport" };

        public void PerformTask()
        {
            DisableErrorReportingViaRegistryEdits();
            RemoveErrorReportingServices();
        }

        private void DisableErrorReportingViaRegistryEdits()
        {
            Console.WriteLine("Writing values into the Registry...");
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
        }

        private void RemoveErrorReportingServices()
        {
            Console.WriteLine("Backing up and removing error reporting services...");
            new ServiceRemover(ERROR_REPORTING_SERVICES)
                .PerformBackup()
                .PerformRemoval();
        }
    }
}
