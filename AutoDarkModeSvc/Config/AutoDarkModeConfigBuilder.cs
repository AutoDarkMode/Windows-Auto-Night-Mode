using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config
{
    public class AutoDarkModeConfigBuilder
    {
        private static AutoDarkModeConfigBuilder instance;
        public AutoDarkModeConfig Config { get; private set; }

        public string ConfigDir { get; }
        public string ConfigFilePath { get; }
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
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
            Directory.CreateDirectory(ConfigDir);
            string jsonConfig = JsonConvert.SerializeObject(Config, Formatting.Indented);
            for (int i = 0; i < 10; i++)
            {
                if (IsFileLocked(new FileInfo(ConfigFilePath)))
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    using StreamWriter writer = new StreamWriter(ConfigFilePath, false);
                    writer.WriteLine(jsonConfig);
                    writer.Close();
                    return;
                }
            }
            throw new TimeoutException("Saving the configuration file failed after 10 retries");
        }

        public void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Save();
            }
            var writeTime = File.GetLastWriteTimeUtc(ConfigFilePath);
            if (writeTime > LastUpdated)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (IsFileLocked(new FileInfo(ConfigFilePath)))
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        using StreamReader reader = File.OpenText(ConfigFilePath);
                        JsonSerializer serializer = new JsonSerializer();
                        var deserializedConfig = (AutoDarkModeConfig)serializer.Deserialize(reader, typeof(AutoDarkModeConfig));
                        Config = deserializedConfig ?? Config;
                        reader.Close();
                        LastUpdated = writeTime;
                        return;
                    }
                }
                throw new TimeoutException("Loading the configuration file failed after 10 retries");
            }
        }

        /// <summary>
        /// Checks if the config file is locked
        /// </summary>
        /// <param name="file">the file to be checked</param>
        /// <returns>true if locked; false otherwise</returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            if (!File.Exists(file.FullName))
            {
                return false;
            }
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {

                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
