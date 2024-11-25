using Microsoft.Win32;
using Win10BloatRemover.UI;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations;

class TelemetryDisabler(IUserInterface ui, ServiceRemover serviceRemover) : IOperation
{
    private static readonly string[] telemetryServices = [
        "DiagTrack",
        "diagsvc",
        "diagnosticshub.standardcollector.service",
        "PcaSvc"
    ];

    private static readonly string[] protectedTelemetryServices = [
        "DPS",
        "WdiSystemHost",
        "WdiServiceHost"
    ];

    private static readonly string[] telemetryScheduledTasks = [
        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
        @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
        @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
        @"\Microsoft\Windows\Application Experience\StartupAppTask",
        @"\Microsoft\Windows\Autochk\Proxy",
        @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
        @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
        @"\Microsoft\Windows\Device Information\Device",
        @"\Microsoft\Windows\Device Information\Device User",
        @"\Microsoft\Windows\NetTrace\GatherNetworkInfo",
        @"\Microsoft\Windows\PI\Sqm-Tasks"
    ];

    public bool IsRebootRecommended { get; private set; }

    public void Run()
    {
        RemoveTelemetryServices();
        DisableTelemetryFeatures();
        DisableTelemetryScheduledTasks();
    }

    private void RemoveTelemetryServices()
    {
        ui.PrintHeading("Backing up and removing telemetry services...");
        serviceRemover.BackupAndRemove(telemetryServices);
        if (serviceRemover.IsRebootRecommended)
            IsRebootRecommended = true;

        string[] actualProtectedServices = serviceRemover.PerformBackup(protectedTelemetryServices);
        RemoveProtectedServices(actualProtectedServices);
    }

    private void RemoveProtectedServices(string[] protectedServicesToRemove)
    {
        using (TokenPrivilege.TakeOwnership)
        {
            using RegistryKey allServicesKey = Registry.LocalMachine.OpenSubKeyWritable(@"SYSTEM\CurrentControlSet\Services");
            foreach (string serviceName in protectedServicesToRemove)
                TryRemoveProtectedService(serviceName, allServicesKey);
        }
    }

    private void TryRemoveProtectedService(string serviceName, RegistryKey allServicesKey)
    {
        try
        {
            RemoveProtectedService(serviceName, allServicesKey);
            ui.PrintMessage($"Service {serviceName} removed, but it will continue to run until the next restart.");
            IsRebootRecommended = true;
        }
        catch (Exception exc)
        {
            ui.PrintError($"Error while trying to delete service {serviceName}: {exc.Message}");
        }
    }

    private void RemoveProtectedService(string serviceName, RegistryKey allServicesKey)
    {
        allServicesKey.GrantFullControlOnSubKey(serviceName);
        using RegistryKey serviceKey = allServicesKey.OpenSubKeyWritable(serviceName);
        foreach (string subkeyName in serviceKey.GetSubKeyNames())
        {
            // Protected subkeys must first be opened only with TakeOwnership right,
            // otherwise the system would prevent us to access them to edit their ACL.
            serviceKey.TakeOwnershipOnSubKey(subkeyName);
            serviceKey.GrantFullControlOnSubKey(subkeyName);
        }
        allServicesKey.DeleteSubKeyTree(serviceName);
    }

    private void DisableTelemetryFeatures()
    {
        ui.PrintHeading("Performing some registry edits to disable telemetry features...");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DisableOneSettingsDownloads", 1);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "DiagnosticData", 0);
        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AppCompat"))
        {
            key.SetValue("AITEnable", 0);   // Application Telemetry
            key.SetValue("DisableInventory", 1);
            key.SetValue("DisablePCA", 1);  // Program Compatibility Assistant
        }
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\WMI\AutoLogger\AutoLogger-Diagtrack-Listener", "Start", 0);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\current\device\System", "AllowExperimentation", 0);
        // Disable sending KMS client activation data to Microsoft automatically
        Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
            "NoGenTicket", 1
        );
        // By setting these keys, we make sure that when someone tries to launch CompatTelRunner or DeviceCensus, the
        // specified Debugger executable is launched instead (see https://devblogs.microsoft.com/oldnewthing/20070702-00/?p=26193).
        // rundll32 was chosen since it does nothing and its exit code is 0 even when launched with invalid parameters.
        string windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe",
            "Debugger", $@"{windowsFolder}\System32\rundll32.exe"
        );
        Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe",
            "Debugger", $@"{windowsFolder}\System32\rundll32.exe"
        );
    }

    private void DisableTelemetryScheduledTasks()
    {
        ui.PrintHeading("Disabling telemetry-related scheduled tasks...");
        new ScheduledTasksDisabler(telemetryScheduledTasks, ui).Run();
    }
}
