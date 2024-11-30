using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover;

class AppConfiguration
{
    public required string[] ServicesToRemove { get; init; }
    public required UwpAppGroup[] UWPAppsToRemove { get; init; }
    public required UwpAppRemovalMode UWPAppsRemovalMode { get; init; }
    public required string[] ScheduledTasksToDisable { get; init; }
    public required string[] WindowsFeaturesToRemove { get; init; }

    private static readonly string configurationFilePath = Path.Join(AppContext.BaseDirectory, "config.json");

    public static AppConfiguration LoadOrCreateFile()
    { 
        if (File.Exists(configurationFilePath))
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
            string settingsFileContent = File.ReadAllText(configurationFilePath, Encoding.UTF8);
            var parsedConfiguration = JsonSerializer.Deserialize(settingsFileContent, AppConfigurationSerializerContext.Default.AppConfiguration);
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
            byte[] settingsFileContent = JsonSerializer.SerializeToUtf8Bytes(this, AppConfigurationSerializerContext.Default.AppConfiguration);
            File.WriteAllBytes(configurationFilePath, settingsFileContent);
        }
        catch (Exception exc)
        {
            throw new AppConfigurationWriteException(exc.Message);
        }
    }

    public static readonly AppConfiguration Default = new() {
        ServicesToRemove = [
            "dmwappushservice",
            "RetailDemo",
            "TroubleshootingSvc"
        ],
        UWPAppsToRemove = [   
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
            UwpAppGroup.Paint3D,
            UwpAppGroup.SolitaireCollection,
            UwpAppGroup.Skype,
            UwpAppGroup.Zune
        ],
        WindowsFeaturesToRemove = [
            "App.StepsRecorder",
            "App.Support.QuickAssist",
            "App.WirelessDisplay.Connect",
            "Browser.InternetExplorer",
            "Hello.Face",
            "MathRecognizer"
        ],
        ScheduledTasksToDisable = [
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
        ],
        UWPAppsRemovalMode = UwpAppRemovalMode.AllUsers
    };
}

[JsonSerializable(typeof(AppConfiguration))]
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    RespectNullableAnnotations = true,
    WriteIndented = true,
    UseStringEnumConverter = true
)]
internal partial class AppConfigurationSerializerContext : JsonSerializerContext {}

abstract class AppConfigurationException(string message) : Exception(message) {}
class AppConfigurationLoadException(string message) : AppConfigurationException(message) {}
class AppConfigurationWriteException(string message) : AppConfigurationException(message) {}
