using Microsoft.Win32;
using System;

namespace Win10BloatRemover.Operations
{
    class CortanaDisabler : IOperation
    {
        public void PerformTask()
        {
            DisableCortanaViaGroupPolicy();
            AddFirewallRuleForCortana();
            Console.WriteLine("A system reboot is recommended.");
        }

        private void DisableCortanaViaGroupPolicy()
        {
            using RegistryKey winSearchPolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
            winSearchPolicies.SetValue("AllowCortana", 0, RegistryValueKind.DWord);
        }

        private void AddFirewallRuleForCortana()
        {
            using RegistryKey firewallRules = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"
            );
            firewallRules.SetValue(
                "{2765E0F4-2918-4A46-B9C9-43CDD8FCBA2B}", "BlockCortana|Action=Block|Active=TRUE|Dir=Out|" +
                @"App=C:\windows\systemapps\microsoft.windows.cortana_cw5n1h2txyewy\searchui.exe|Name=Search and Cortana application|" +
                "AppPkgId=S-1-15-2-1861897761-1695161497-2927542615-642690995-327840285-2659745135-2630312742|", RegistryValueKind.String
            );
        }
    }
}
