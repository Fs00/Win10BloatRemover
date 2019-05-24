using Microsoft.Win32;
using System;
using Win10BloatRemover.Utils;

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
            ConsoleUtils.WriteLine("Writing values into the Registry...", ConsoleColor.Green);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting"))
                key.SetValue("Disabled", 1, RegistryValueKind.DWord);
        }

        private void RemoveErrorReportingServices()
        {
            ConsoleUtils.WriteLine("Backing up and removing error reporting services...", ConsoleColor.Green);
            new ServiceRemover(ERROR_REPORTING_SERVICES)
                .PerformBackup()
                .PerformRemoval();
        }
    }
}
