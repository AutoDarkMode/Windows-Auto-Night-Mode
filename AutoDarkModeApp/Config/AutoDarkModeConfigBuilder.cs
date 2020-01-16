using Newtonsoft.Json;
using System;
using System.IO;

namespace AutoDarkModeApp.Config
{
    public class AutoDarkModeConfigBuilder
    {
        private static AutoDarkModeConfigBuilder instance;
        public AutoDarkModeConfig Config { get; private set; }

        private string ConfigDir { get; }
        private string ConfigFilePath { get; }
        protected AutoDarkModeConfigBuilder()
        {
            if (instance == null)
            {
                Config = new AutoDarkModeConfig();
                ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
                ConfigFilePath = Path.Combine(ConfigDir, "config.json");
            }
        }

        public static AutoDarkModeConfigBuilder Instance()
        {
            if (instance == null)
            {
                instance = new AutoDarkModeConfigBuilder();
            }
            return instance;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string jsonConfig = JsonConvert.SerializeObject(Config, Formatting.Indented);
                using StreamWriter writer = new StreamWriter(ConfigFilePath, false);
                writer.WriteLine(jsonConfig);
                writer.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Save();
            }
            try
            {
                using StreamReader reader = File.OpenText(ConfigFilePath);
                JsonSerializer serializer = new JsonSerializer();
                Config = (AutoDarkModeConfig)serializer.Deserialize(reader, typeof(AutoDarkModeConfig));
                reader.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
