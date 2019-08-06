using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Resources;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    class ConfigurationException : Exception
    {
        public ConfigurationException() {}
        public ConfigurationException(string message) : base(message) {}
        public ConfigurationException(string message, Exception inner) : base(message, inner) {}
    }

    /*
     *  Singleton class that stores user-configurable data, which are loaded from a JSON file
     */
    class Configuration
    {
        public static Configuration Instance { private set; get; }
        private const string CONFIGURATION_FILE_NAME = "config.json";

        // Singleton initializer: must be called at program startup
        // Only ConfigurationExceptions thrown by this method should be handled (see below)
        public static void Load()
        {
            // Failure while parsing default settings should make the program crash
            string defaultSettingsFileContent = LoadDefaultSettings();

            if (File.Exists(CONFIGURATION_FILE_NAME))
                TryLoadConfigFromFile();
            else
                WriteDefaultSettingsToFile(defaultSettingsFileContent);
        }

        private static string LoadDefaultSettings()
        {
            var defaultSettingsFileContent = (string) new ResourceManager("Win10BloatRemover.resources.Resources",
                                             typeof(Configuration).Assembly).GetObject(CONFIGURATION_FILE_NAME);
            Instance = JsonConvert.DeserializeObject<Configuration>(defaultSettingsFileContent);
            return defaultSettingsFileContent;
        }

        private static void TryLoadConfigFromFile()
        {
            try
            {
                Configuration fileParsingResult =
                    JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(CONFIGURATION_FILE_NAME));
                Instance = fileParsingResult;
            }
            catch (Exception exc)
            {
                throw new ConfigurationException($"Error when loading custom settings file: {exc.Message}\n" +
                                                 "Default settings have been loaded instead.\n");
            }
        }

        private static void WriteDefaultSettingsToFile(string defaultSettingsFileContent)
        {
            try
            {
                File.WriteAllText(CONFIGURATION_FILE_NAME, defaultSettingsFileContent);
            }
            catch (Exception exc)
            {
                throw new ConfigurationException($"Can't write configuration file with default settings: {exc.Message}\n");
            }
        }

        [JsonProperty(Required = Required.Always)]
        public string[] ServicesToRemove { private set; get; }

        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public UWPAppGroup[] UWPAppsToRemove { private set; get; }

        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public UWPAppRemovalMode UWPAppsRemovalMode { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] ScheduledTasksToDisable { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] WindowsFeaturesToRemove { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public bool AllowInstallWimTweak { private set; get; }
    }
}
