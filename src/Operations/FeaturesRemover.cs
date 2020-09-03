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
                foreach (string capabilityName in featuresToRemove)
                    RemoveCapabilitiesWhoseNameStartsWith(capabilityName);
            }

            ui.PrintNotice("A system reboot is recommended.");
        }

        private void RemoveCapabilitiesWhoseNameStartsWith(string capabilityName)
        {
            var capabilities = powerShell.Run($"Get-WindowsCapability -Online -Name {capabilityName}*");
            if (capabilities.Length == 0)
            {
                ui.PrintWarning($"No features found with name {capabilityName}.");
                return;
            }

            foreach (var capability in capabilities)
                RemoveCapability(capability);
        }

        private void RemoveCapability(dynamic capability)
        {
            if (capability.State.ToString() != "Installed")
            {
                ui.PrintMessage($"Feature {capability.Name} is not installed.");
                return;
            }

            ui.PrintMessage($"Removing feature {capability.Name}...");
            powerShell.Run($"Remove-WindowsCapability -Online -Name {capability.Name}");

            if (capability.Name.StartsWith("Hello.Face"))
                new ScheduledTasksDisabler(new[] { @"\Microsoft\Windows\HelloFace\FODCleanupTask" }, ui).Run();
        }
    }
}
