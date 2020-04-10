using System.Management.Automation;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class FeaturesRemover : IOperation
    {
        private readonly string[] featuresToRemove;
        private readonly IUserInterface ui;

        public FeaturesRemover(string[] featuresToRemove, IUserInterface ui)
        {
            this.featuresToRemove = featuresToRemove;
            this.ui = ui;
        }

        public void Run()
        {
            string removalScript = "";
            foreach (string featureName in featuresToRemove)
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

            using PowerShell psInstance = PowerShellExtensions.CreateWithImportedModules("Dism");
            psInstance.RunScriptAndPrintOutput(removalScript, ui);

            ui.PrintMessage("A system reboot is recommended.");
        }
    }
}
