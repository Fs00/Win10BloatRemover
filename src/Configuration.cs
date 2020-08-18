using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    class ConfigurationException : Exception
    {
        public ConfigurationException() {}
        public ConfigurationException(string message) : base(message) {}
        public ConfigurationException(string message, Exception inner) : base(message, inner) {}
    }

    public class Configuration
    {
        private const string CONFIGURATION_FILE_NAME = "config.json";

        #nullable disable warnings
        [JsonProperty(Required = Required.Always)]
        public string[] ServicesToRemove { private set; get; }

        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public UWPAppGroup[] UWPAppsToRemove { private set; get; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public UWPAppRemovalMode UWPAppsRemovalMode { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] ScheduledTasksToDisable { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] WindowsFeaturesToRemove { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public bool AllowInstallWimTweak { private set; get; }
        #nullable restore warnings

        public static Configuration LoadFromFileOrDefault()
        { 
            if (File.Exists(CONFIGURATION_FILE_NAME))
                return ParseConfigFile();

            Default.WriteToFile();
            return Default;
        }

        private static Configuration ParseConfigFile()
        {
            try
            {
                var parsedConfiguration =
                    JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(CONFIGURATION_FILE_NAME));
                return parsedConfiguration;
            }
            catch (Exception exc)
            {
                throw new ConfigurationException($"Error when loading custom settings file: {exc.Message}\n" +
                                                 "Default settings have been loaded instead.\n");
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
                throw new ConfigurationException($"Can't write configuration file with default settings: {exc.Message}\n");
            }
        }

        public static readonly Configuration Default = new Configuration {
            ServicesToRemove = new[] {
                "dmwappushservice",
                "RetailDemo",
                "TroubleshootingSvc"
            },
            UWPAppsToRemove = new[] {   
                UWPAppGroup.Bing,
                UWPAppGroup.Cortana,
                UWPAppGroup.CommunicationsApps,
                UWPAppGroup.OneNote,
                UWPAppGroup.OfficeHub,
                UWPAppGroup.HelpAndFeedback,
                UWPAppGroup.Maps,
                UWPAppGroup.Messaging,
                UWPAppGroup.Mobile,
                UWPAppGroup.Skype,
                UWPAppGroup.Zune
            },
            WindowsFeaturesToRemove = new[] {
                "Microsoft-Windows-InternetExplorer",
                "Microsoft-Windows-Hello-Face",
                "Microsoft-Windows-QuickAssist",
                "Microsoft-Windows-TabletPCMath",
                "Microsoft-Windows-StepsRecorder",
                "Microsoft-Windows-WirelessDisplay"
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
            UWPAppsRemovalMode = UWPAppRemovalMode.AllUsers,
            AllowInstallWimTweak = false
        };
    }
}
