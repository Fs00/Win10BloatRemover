using Microsoft.Win32;
using System;

namespace Win10BloatRemover.Operations
{
    class AutoUpdatesDisabler : IOperation
    {
        public void PerformTask()
        {
            Console.WriteLine("Writing values into the Registry...");
            DisableAutomaticWindowsUpdates();
            DisableAutomaticStoreUpdates();
        }

        private void DisableAutomaticWindowsUpdates()
        {
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
        }

        private void DisableAutomaticStoreUpdates()
        {
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey key = localMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore");
            key.SetValue("AutoDownload", 2, RegistryValueKind.DWord);
        }
    }
}
