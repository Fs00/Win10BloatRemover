using System.Management.Automation;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    public class FeaturesRemover : IOperation
    {
        private readonly string[] featuresToRemove;
        private readonly IUserInterface ui;

        private /*lateinit*/ PowerShell powerShell;

        #nullable disable warnings
        public FeaturesRemover(string[] featuresToRemove, IUserInterface ui)
        {
            this.featuresToRemove = featuresToRemove;
            this.ui = ui;
        }
        #nullable restore warnings

        public void Run()
        {
            using (powerShell = PowerShellExtensions.CreateWithImportedModules("Dism").WithOutput(ui))
            {
                foreach (string featureName in featuresToRemove)
                    RemoveFeaturesWhoseNameStartsWith(featureName);
            }

            ui.PrintMessage("A system reboot is recommended.");
        }

        private void RemoveFeaturesWhoseNameStartsWith(string featureName)
        {
            var featurePackages = powerShell.Run($"Get-WindowsPackage -Online -PackageName {featureName}*");
            if (featurePackages.Length > 0)
            {
                foreach (var package in featurePackages)
                {
                    ui.PrintMessage($"Removing feature {package.PackageName}...");
                    powerShell.Run($"Remove-WindowsPackage -Online -NoRestart -PackageName {package.PackageName}");
                }

                if (featureName.Contains("Windows-Hello-Face"))
                    new ScheduledTasksDisabler(new[] { @"\Microsoft\Windows\HelloFace\FODCleanupTask" }, ui).Run();
            }
            else
                ui.PrintMessage($"Feature {featureName} is not installed.");
        }
    }
}
