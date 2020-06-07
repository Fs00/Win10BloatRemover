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
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
        }

        private void DisableAutomaticStoreUpdates()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2);
        }
    }
}
