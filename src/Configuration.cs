using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Resources;
using Win10BloatRemover.Operations;

namespace Win10BloatRemover
{
    /**
     *  Configuration
     *  Singleton class that stores user-configurable data, which are loaded from a JSON file
     */
    class Configuration
    {
        public static Configuration Instance { private set; get; }

        /**
         *  Initializes the singleton by loading settings from the config.json file found in current directory
         *  If the file doesn't exist, it is created firstly using default settings
         */
        public static string Load()
        {
            string errorMessage = null;
            string configurationFile = "./config.json";
            var defaultSettings = (string) new ResourceManager("Win10BloatRemover.resources.Resources",
                                  typeof(Configuration).Assembly).GetObject("config.json");

            if (!File.Exists(configurationFile))
            {
                try
                {
                    File.WriteAllText(configurationFile, defaultSettings);
                }
                catch (Exception exc)
                {
                    errorMessage += $"Can't write configuration file with default settings: {exc.Message}\n";
                }
            }

            // If loading settings from config.json file fails, default settings are loaded
            try
            {
                Instance = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationFile),
                           new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });  // this setting seems not to work (why?)
            }
            catch (Exception exc)
            {
                errorMessage += $"Error when loading custom settings file: {exc.Message}\nDefault settings have been loaded instead.\n";
                Instance = JsonConvert.DeserializeObject<Configuration>(defaultSettings);
            }
            return errorMessage;
        }

        [JsonProperty(Required = Required.Always)]
        public string[] ServicesToRemove { private set; get; }

        [JsonProperty(Required = Required.Always, ItemConverterType = typeof(StringEnumConverter))]
        public UWPAppGroup[] UWPAppsToRemove { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] ScheduledTasksToDisable { private set; get; }

        [JsonProperty(Required = Required.Always)]
        public string[] WindowsFeaturesToRemove { private set; get; }
    }
}
