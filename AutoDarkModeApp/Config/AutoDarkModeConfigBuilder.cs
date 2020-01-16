using Newtonsoft.Json;
using System;
using System.IO;

namespace AutoDarkModeApp.Config
{
    public class AutoDarkModeConfigBuilder
    {
        private static AutoDarkModeConfigBuilder instance;
        public AutoDarkModeConfig config { get; private set; }

        private const string configFileName = "AutoDarkModeConfig.json";
        protected AutoDarkModeConfigBuilder()
        {
            if (instance == null)
            {
                config = new AutoDarkModeConfig();
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
                string jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
                using StreamWriter writer = new StreamWriter(Path.Combine(Environment.CurrentDirectory, configFileName), false);
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
            string path = Path.Combine(Environment.CurrentDirectory, configFileName);
            if (!File.Exists(path))
            {
                Save();
            }
            try
            {
                using StreamReader reader = File.OpenText(path);
                JsonSerializer serializer = new JsonSerializer();
                config = (AutoDarkModeConfig)serializer.Deserialize(reader, typeof(AutoDarkModeConfig));
                reader.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
