using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeConfig
{
    public class AdmConfigBuilder
    {
        private static AdmConfigBuilder instance;
        public AdmConfig Config { get; private set; }
        public AdmLocationData LocationData { get; private set; }
        public string ConfigDir { get; }
        public string ConfigFilePath { get; }
        public string LocationDataPath { get; }
        protected AdmConfigBuilder()
        {
            if (instance == null)
            {
                Config = new AdmConfig();
                LocationData = new AdmLocationData();
                ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
                ConfigFilePath = Path.Combine(ConfigDir, "config.json");
                LocationDataPath = Path.Combine(ConfigDir, "location_data.json");
            }
        }

        public static AdmConfigBuilder Instance()
        {
            if (instance == null)
            {
                instance = new AdmConfigBuilder();
            }
            return instance;
        }

        public void Save()
        {
            SaveConfig(ConfigFilePath, Config);
        }

        public void SaveLocationData()
        {
            SaveConfig(LocationDataPath, LocationData);
        }

        private void SaveConfig(string path, object obj)
        {
            Directory.CreateDirectory(ConfigDir);
            string jsonConfig = JsonConvert.SerializeObject(obj, Formatting.Indented);
            for (int i = 0; i < 10; i++)
            {
                if (IsFileLocked(new FileInfo(path)))
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    using StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read));
                    writer.WriteLine(jsonConfig);
                    writer.Close();
                    return;
                }
            }
            throw new TimeoutException($"Saving to {path} failed after 10 retries");
        }

        public void LoadLocationData()
        {
            if (!File.Exists(LocationDataPath))
            {
                SaveLocationData();
            }

            using StreamReader locationDataReader = new StreamReader(File.Open(LocationDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            JsonSerializer serializer = new JsonSerializer();
            AdmLocationData deserializedLocationData = (AdmLocationData)serializer.Deserialize(locationDataReader, typeof(AdmLocationData));
            LocationData = deserializedLocationData ?? LocationData;
        }

        public void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Save();
            }

            using StreamReader configReader = new StreamReader(File.Open(ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            JsonSerializer serializer = new JsonSerializer();
            AdmConfig deserializedConfig = (AdmConfig)serializer.Deserialize(configReader, typeof(AdmConfig));
            Config = deserializedConfig ?? Config;
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
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
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
