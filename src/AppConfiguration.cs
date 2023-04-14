using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover;

public class AppConfiguration
{
    private const string CONFIGURATION_FILE_NAME = "config.json";

    #nullable disable warnings
    [JsonProperty(Required = Required.Always)]
    public string[] ServicesToRemove { private set; get; }

    [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
    public UwpAppGroup[] UWPAppsToRemove { private set; get; }

    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public UwpAppRemovalMode UWPAppsRemovalMode { private set; get; }

    [JsonProperty(Required = Required.Always)]
    public string[] ScheduledTasksToDisable { private set; get; }

    [JsonProperty(Required = Required.Always)]
    public string[] WindowsFeaturesToRemove { private set; get; }
    #nullable restore warnings

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
            string settingsFileContent = File.ReadAllText(CONFIGURATION_FILE_NAME);
            var parsedConfiguration = JsonConvert.DeserializeObject<AppConfiguration>(settingsFileContent);
            if (parsedConfiguration == null)
                throw new Exception("The file is empty.");
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
            string settingsFileContent = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(CONFIGURATION_FILE_NAME, settingsFileContent);
        }
        catch (Exception exc)
        {
            throw new AppConfigurationWriteException(exc.Message);
        }
    }

    public static readonly AppConfiguration Default = new AppConfiguration {
        ServicesToRemove = new[] {
            "dmwappushservice",
            "RetailDemo",
            "TroubleshootingSvc"
        },
        UWPAppsToRemove = new[] {   
            UwpAppGroup.Bing,
            UwpAppGroup.Cortana,
            UwpAppGroup.CommunicationsApps,
            UwpAppGroup.OneNote,
            UwpAppGroup.OfficeHub,
            UwpAppGroup.HelpAndFeedback,
            UwpAppGroup.Maps,
            UwpAppGroup.Messaging,
            UwpAppGroup.Mobile,
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
