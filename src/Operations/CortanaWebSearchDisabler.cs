using Microsoft.Win32;
using System;

namespace Win10BloatRemover.Operations
{
    class CortanaWebSearchDisabler : IOperation
    {
        public void PerformTask()
        {
            DisableCortanaAndWebSearchViaGroupPolicies();
            AddFirewallRuleForCortana();
            Console.WriteLine("A system reboot is recommended.");
        }

        private void DisableCortanaAndWebSearchViaGroupPolicies()
        {
            using RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
            key.SetValue("AllowCortana", 0, RegistryValueKind.DWord);
            key.SetValue("AllowSearchToUseLocation", 0, RegistryValueKind.DWord);
            key.SetValue("DisableWebSearch", 1, RegistryValueKind.DWord);
            key.SetValue("ConnectedSearchUseWeb", 0, RegistryValueKind.DWord);
        }

        private void AddFirewallRuleForCortana()
        {
            using RegistryKey firewallRules = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"
            );
            firewallRules.SetValue(
                "{2765E0F4-2918-4A46-B9C9-43CDD8FCBA2B}", "v2.30|Action=Block|Active=TRUE|Dir=Out|" +
                @"App=%windir%\SystemApps\Microsoft.Windows.Cortana_cw5n1h2txyewy\searchUI.exe|Name=Block Search and Cortana|",
                RegistryValueKind.String
            );
        }
    }
}
