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
            DisableAutomaticSpeechModelUpdates();
        }

        private void DisableAutomaticWindowsUpdates()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 1);
        }

        private void DisableAutomaticStoreUpdates()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2);
            // The above policy does not work on Windows 10 Home, so we need to change the Store app setting
            // to disable automatic updates for all users
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate",
                "AutoDownload", 2
            );
        }

        private void DisableAutomaticSpeechModelUpdates()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Speech", "AllowSpeechModelUpdate", 0);
        }
    }
}
