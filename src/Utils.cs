using System;
using Microsoft.Win32;

namespace Win10BloatRemover
{
    /**
     *  Utils
     *  Contains functions that perform tasks which don't belong to a particular category
     */
    static class Utils
    {
        public static void DisableCortana()
        {
            // Set group policy to disable Cortana
            using (RegistryKey winSearchPolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                winSearchPolicies.SetValue("AllowCortana", 0, RegistryValueKind.DWord);

            // Add firewall rule to prevent Cortana connecting to Internet
            using (RegistryKey firewallRules = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"))
                firewallRules.SetValue("{2765E0F4-2918-4A46-B9C9-43CDD8FCBA2B}", "BlockCortana|Action=Block|Active=TRUE|Dir=Out|" +
                                        @"App=C:\windows\systemapps\microsoft.windows.cortana_cw5n1h2txyewy\searchui.exe|Name=Search and Cortana application|" +
                                        "AppPkgId=S-1-15-2-1861897761-1695161497-2927542615-642690995-327840285-2659745135-2630312742|", RegistryValueKind.String);
        }

        public static void DisableAutomaticUpdates()
        {
            // SEEMS NOT TO WORK AS INTENDED, MORE INVESTIGATIONS TO BE DONE
            using (RegistryKey winUpdatePolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                winUpdatePolicies.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
        }
    }
}
