using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover;

public class AppConfiguration
{
    private const string CONFIGURATION_FILE_NAME = "config.json";

    public required string[] ServicesToRemove { get; init; }
    public required UwpAppGroup[] UWPAppsToRemove { get; init; }
    public required UwpAppRemovalMode UWPAppsRemovalMode { get; init; }
    public required string[] ScheduledTasksToDisable { get; init; }
    public required string[] WindowsFeaturesToRemove { get; init; }

    private static readonly JsonSerializerOptions serializerOptions = new() {
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public static AppConfiguration LoadOrCreateFile()
    { 
        if (File.Exists(CONFIGURATION_FILE_NAME))
        {
            var loadedConfiguration = LoadFromFile();
            return loadedConfiguration;
        }

        Default.WriteToFile();
        return Default;
    }

    private static AppConfiguration LoadFromFile()
    {
        try
        {
            string settingsFileContent = File.ReadAllText(CONFIGURATION_FILE_NAME, Encoding.UTF8);
            var parsedConfiguration = JsonSerializer.Deserialize<AppConfiguration>(settingsFileContent, serializerOptions);
            if (parsedConfiguration == null)
                throw new Exception("The file does not contain a valid configuration.");
            return parsedConfiguration;
        }
        catch (Exception exc)
        {
            throw new AppConfigurationLoadException(exc.Message);
        }
    }

    private void WriteToFile()
    {
        try
        {
            byte[] settingsFileContent = JsonSerializer.SerializeToUtf8Bytes(this, serializerOptions);
            File.WriteAllBytes(CONFIGURATION_FILE_NAME, settingsFileContent);
        }
        catch (Exception exc)
        {
            throw new AppConfigurationWriteException(exc.Message);
        }
    }

    public static readonly AppConfiguration Default = new() {
        ServicesToRemove = new[] {
            "dmwappushservice",
            "RetailDemo",
            "TroubleshootingSvc"
        },
        UWPAppsToRemove = new[] {   
            UwpAppGroup.Bing,
            UwpAppGroup.Cortana,
            UwpAppGroup.CommunicationsApps,
            UwpAppGroup.HelpAndFeedback,
            UwpAppGroup.Maps,
            UwpAppGroup.Messaging,
            UwpAppGroup.MixedReality,
            UwpAppGroup.Mobile,
            UwpAppGroup.OneNote,
            UwpAppGroup.OfficeHub,
            UwpAppGroup.SolitaireCollection,
            UwpAppGroup.Skype,
            UwpAppGroup.Zune
        },
        WindowsFeaturesToRemove = new[] {
            "App.StepsRecorder",
            "App.Support.QuickAssist",
            "App.WirelessDisplay.Connect",
            "Browser.InternetExplorer",
            "Hello.Face",
            "MathRecognizer"
        },
        ScheduledTasksToDisable = new[] {
            @"\Microsoft\Windows\ApplicationData\DsSvcCleanup",
            @"\Microsoft\Windows\CloudExperienceHost\CreateObjectTask",
            @"\Microsoft\Windows\DiskFootprint\Diagnostics",
            @"\Microsoft\Windows\Maintenance\WinSAT",
            @"\Microsoft\Windows\Shell\FamilySafetyMonitor",
            @"\Microsoft\Windows\Shell\FamilySafetyRefreshTask",
            @"\Microsoft\Windows\License Manager\TempSignedLicenseExchange",
            @"\Microsoft\Windows\Clip\License Validation",
            @"\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem",
            @"\Microsoft\Windows\PushToInstall\LoginCheck",
            @"\Microsoft\Windows\PushToInstall\Registration",
            @"\Microsoft\Windows\Subscription\EnableLicenseAcquisition",
            @"\Microsoft\Windows\Subscription\LicenseAcquisition",
            @"\Microsoft\Windows\Diagnosis\Scheduled",
            @"\Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner"
        },
        UWPAppsRemovalMode = UwpAppRemovalMode.AllUsers
    };
}

abstract class AppConfigurationException : Exception
{
    protected AppConfigurationException(string message) : base(message) {}
}

class AppConfigurationLoadException : AppConfigurationException
{
    public AppConfigurationLoadException(string message) : base(message) {}
}

class AppConfigurationWriteException : AppConfigurationException
{
    public AppConfigurationWriteException(string message) : base(message) {}
}
