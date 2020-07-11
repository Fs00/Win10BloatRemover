using Microsoft.Win32;

namespace Win10BloatRemover.Operations
{
    public class CortanaDisabler : IOperation
    {
        private readonly IUserInterface ui;
        public CortanaDisabler(IUserInterface ui) => this.ui = ui;

        public void Run()
        {
            DisableCortana();
            AddFirewallRuleToBlockCortana();
            ui.PrintMessage("A system reboot is recommended.");
        }

        private void DisableCortana()
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        }

        private void AddFirewallRuleToBlockCortana()
        {
            Registry.SetValue(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules",
                "{2765E0F4-2918-4A46-B9C9-43CDD8FCBA2B}",
                "v2.30|Action=Block|Active=TRUE|Dir=Out|" +
                @"App=%windir%\SystemApps\Microsoft.Windows.Cortana_cw5n1h2txyewy\searchUI.exe|Name=Block Search and Cortana|"
            );
        }
    }
}
