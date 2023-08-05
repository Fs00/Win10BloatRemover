using Microsoft.Dism;

namespace Win10BloatRemover.Utils;

class DismClient : IDisposable
{
    private readonly Lazy<DismSession> dismSession = new Lazy<DismSession>(() => {
        DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo);
        return DismApi.OpenOnlineSessionEx(new DismSessionOptions { ThrowExceptionOnRebootRequired = false });
    });

    public void Dispose()
    {
        if (dismSession.IsValueCreated)
        {
            dismSession.Value.Dispose();
            DismApi.Shutdown();
        }
    }

    public bool IsRebootRequired => dismSession.IsValueCreated && dismSession.Value.RebootRequired;

    public DismCapabilityCollection GetCapabilities()
    {
        return DismApi.GetCapabilities(dismSession.Value);
    }

    public void RemoveCapability(string capabilityName)
    {
        DismApi.RemoveCapability(dismSession.Value, capabilityName);
    }

    public DismAppxPackage? FindAppxProvisionedPackageByName(string displayName)
    {
        return DismApi.GetProvisionedAppxPackages(dismSession.Value)
            .FirstOrDefault(package => package.DisplayName == displayName);
    }

    public void RemoveAppxProvisionedPackage(string packageName)
    {
        DismApi.RemoveProvisionedAppxPackage(dismSession.Value, packageName);
    }
}
