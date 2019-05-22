using System;
using System.Management.Automation;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class FeaturesRemover : IOperation
    {
        public void PerformTask()
        {
            string removalScript = "";
            foreach (string featureName in Configuration.Instance.WindowsFeaturesToRemove)
            {
                removalScript += $"$feature = Get-WindowsPackage -Online -PackageName *{featureName}*;" +
                                 "if ($feature) {" +
                                    "Write-Host \"Removing feature $($feature.PackageName)...\";" +
                                    "Remove-WindowsPackage -Online -NoRestart -PackageName $feature.PackageName;" +
                                 "}" +
                                 "else" +
                                    "{ Write-Host \"Feature " + featureName + " is not installed.\"; }";

                if (featureName == "Hello-Face-Package")
                    removalScript += "schtasks /Change /TN \"\\Microsoft\\Windows\\HelloFace\\FODCleanupTask\" /Disable;";
            }

            using (PowerShell psInstance = PowerShell.Create())
                psInstance.RunScriptAndPrintOutput(removalScript);

            Console.WriteLine("A system reboot is recommended.");
        }
    }
}
