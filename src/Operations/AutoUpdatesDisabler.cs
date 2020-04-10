using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class AutoUpdatesDisabler : IOperation
    {
        private readonly IUserInterface ui;
        public AutoUpdatesDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            ui.PrintMessage("Writing values into the Registry...");
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
