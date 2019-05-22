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
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
        }

        private void DisableAutomaticStoreUpdates()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore"))
                key.SetValue("AutoDownload", 2, RegistryValueKind.DWord);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\WOW6432Node\Policies\Microsoft\WindowsStore"))
                key.SetValue("AutoDownload", 2, RegistryValueKind.DWord);
        }
    }
}
