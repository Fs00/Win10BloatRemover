using Microsoft.Dism;
using System;
using System.Linq;
using Win10BloatRemover.UI;

namespace Win10BloatRemover.Operations;

public class FeaturesRemover : IOperation
{
    private readonly string[] featuresToRemove;
    private readonly IUserInterface ui;

    public bool IsRebootRecommended { get; private set; }

    public FeaturesRemover(string[] featuresToRemove, IUserInterface ui)
    {
        this.featuresToRemove = featuresToRemove;
        this.ui = ui;
    }

    public void Run()
    {
        DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo);
        try
        {
            using var session = DismApi.OpenOnlineSessionEx(new DismSessionOptions { ThrowExceptionOnRebootRequired = false });
            var capabilities = DismApi.GetCapabilities(session);
            foreach (string featureName in featuresToRemove)
                RemoveCapabilitiesMatchingName(featureName, capabilities, session);

            IsRebootRecommended = session.RebootRequired;
        }
        finally
        {
            DismApi.Shutdown();
        }
    }

    private void RemoveCapabilitiesMatchingName(string featureName, DismCapabilityCollection capabilities, DismSession session)
    {
        var matchingCapabilities = capabilities.Where(capability => capability.Name.StartsWith(featureName));
        if (!matchingCapabilities.Any())
        {
            ui.PrintWarning($"No features found with name {featureName}.");
            return;
        }

        foreach (var capability in matchingCapabilities)
            TryRemoveCapability(capability, session);
    }

    private void TryRemoveCapability(DismCapability capability, DismSession session)
    {
        if (capability.State != DismPackageFeatureState.Installed)
        {
            ui.PrintMessage($"Feature {capability.Name} is not installed.");
            return;
        }

        try
        {
            ui.PrintMessage($"Removing feature {capability.Name}...");
            DismApi.RemoveCapability(session, capability.Name);

            if (capability.Name.StartsWith("Hello.Face"))
                new ScheduledTasksDisabler(new[] { @"\Microsoft\Windows\HelloFace\FODCleanupTask" }, ui).Run();
        }
        catch (Exception exc)
        {
            ui.PrintError($"Feature {capability.Name} could not be removed: {exc.Message}");
        }
    }
}
