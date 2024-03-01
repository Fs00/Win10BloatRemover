using Microsoft.Dism;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations;

public class FeaturesRemover(string[] featuresToRemove, IUserInterface ui) : IOperation
{
    public bool IsRebootRecommended { get; private set; }

    public void Run()
    {
        using var dismClient = new DismClient();
        var allCapabilities = dismClient.GetCapabilities();
        foreach (string featureName in featuresToRemove)
            RemoveCapabilitiesMatchingName(featureName, allCapabilities, dismClient);

        IsRebootRecommended = dismClient.IsRebootRequired;
    }

    private void RemoveCapabilitiesMatchingName(string featureName, DismCapabilityCollection allCapabilities, DismClient dismClient)
    {
        var matchingCapabilities = allCapabilities.Where(capability => capability.Name.StartsWith(featureName));
        if (!matchingCapabilities.Any())
        {
            ui.PrintWarning($"No features found with name {featureName}.");
            return;
        }

        foreach (var capability in matchingCapabilities)
            TryRemoveCapability(capability, dismClient);
    }

    private void TryRemoveCapability(DismCapability capability, DismClient dismClient)
    {
        if (capability.State != DismPackageFeatureState.Installed)
        {
            ui.PrintMessage($"Feature {capability.Name} is not installed.");
            return;
        }

        try
        {
            ui.PrintMessage($"Removing feature {capability.Name}...");
            dismClient.RemoveCapability(capability.Name);

            if (capability.Name.StartsWith("Hello.Face"))
                new ScheduledTasksDisabler([@"\Microsoft\Windows\HelloFace\FODCleanupTask"], ui).Run();
        }
        catch (Exception exc)
        {
            ui.PrintError($"Feature {capability.Name} could not be removed: {exc.Message}");
        }
    }
}
