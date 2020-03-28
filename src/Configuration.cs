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

    class Configuration
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
                UWPAppGroup.Zune,
                UWPAppGroup.CommunicationsApps,
                UWPAppGroup.OneNote,
                UWPAppGroup.OfficeHub,
                UWPAppGroup.Camera,
                UWPAppGroup.Maps,
                UWPAppGroup.Mobile,
                UWPAppGroup.HelpAndFeedback,
                UWPAppGroup.Bing,
                UWPAppGroup.Messaging,
                UWPAppGroup.Skype
            },
            WindowsFeaturesToRemove = new[] {
                "InternetExplorer-Optional-Package",
                "Hello-Face-Package",
                "QuickAssist-Package"
            },
            ScheduledTasksToDisable = new[] {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Application Experience\StartupAppTask",
                @"\Microsoft\Windows\ApplicationData\DsSvcCleanup",
                @"\Microsoft\Windows\Autochk\Proxy",
                @"\Microsoft\Windows\CloudExperienceHost\CreateObjectTask",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                @"\Microsoft\Windows\DiskFootprint\Diagnostics",
                @"\Microsoft\Windows\Device Information\Device",
                @"\Microsoft\Windows\FileHistory\File History (maintenance mode)",
                @"\Microsoft\Windows\Maintenance\WinSAT",
                @"\Microsoft\Windows\PI\Sqm-Tasks",
                @"\Microsoft\Windows\Shell\FamilySafetyMonitor",
                @"\Microsoft\Windows\Shell\FamilySafetyRefreshTask",
                @"\Microsoft\Windows\Windows Error Reporting\QueueReporting",
                @"\Microsoft\Windows\License Manager\TempSignedLicenseExchange",
                @"\Microsoft\Windows\Clip\License Validation",
                @"\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem",
                @"\Microsoft\Windows\PushToInstall\LoginCheck",
                @"\Microsoft\Windows\PushToInstall\Registration",
                @"\Microsoft\Windows\Subscription\EnableLicenseAcquisition",
                @"\Microsoft\Windows\Subscription\LicenseAcquisition",
                @"\Microsoft\Windows\Diagnosis\Scheduled",
                @"\Microsoft\Windows\NetTrace\GatherNetworkInfo",
                @"\Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner"
            },
            UWPAppsRemovalMode = UWPAppRemovalMode.RemoveProvisionedPackages,
            AllowInstallWimTweak = false
        };
    }
}
