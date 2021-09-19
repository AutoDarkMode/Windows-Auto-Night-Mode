using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


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
        public bool Loading { get; private set; }
        protected AdmConfigBuilder()
        {
            if (instance == null)
            {
                Config = new AdmConfig();
                LocationData = new AdmLocationData();
                ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
                ConfigFilePath = Path.Combine(ConfigDir, "config.yaml");
                LocationDataPath = Path.Combine(ConfigDir, "location_data.yaml");
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
            //string jsonConfig = JsonConvert.SerializeObject(obj, Formatting.Indented);
            var yamlSerializer = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            string yamlConfig = yamlSerializer.Serialize(obj);
            for (int i = 0; i < 10; i++)
            {
                if (IsFileLocked(new FileInfo(path)))
                {
                    Thread.Sleep(500);
                }
                else
                {
                    using StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write));
                    writer.WriteLine(yamlConfig);
                    writer.Close();
                    return;
                }
            }
            throw new TimeoutException($"Saving to {path} failed after 10 retries");
        }

        private string LoadFile(string path)
        {
            Loading = true;
            Exception readException = new TimeoutException($"Reading from {path} failed after 3 retries");
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using StreamReader dataReader = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    return dataReader.ReadToEnd();
                }
                catch (Exception ex) {
                    readException = ex;
                    Thread.Sleep(500);
                }
            }
            throw readException;
        }

        public void LoadLocationData()
        {
            Loading = true;
            if (!File.Exists(LocationDataPath))
            {
                SaveLocationData();
            }

            //using StreamReader locationDataReader = new(File.Open(LocationDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //JsonSerializer serializer = new JsonSerializer();
            //AdmLocationData deserializedLocationData = (AdmLocationData)serializer.Deserialize(locationDataReader, typeof(AdmLocationData));
            var yamlDeserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            AdmLocationData deserializedLocationDataYaml = yamlDeserializer.Deserialize<AdmLocationData>(LoadFile(LocationDataPath));
            LocationData = deserializedLocationDataYaml ?? LocationData;
            Loading = false;
        }

        public void Load()
        {
            Loading = true;
            if (!File.Exists(ConfigFilePath))
            {
                Save();
            }
            //var data = ReadWithRetry(ConfigFilePath);
            //using StreamReader dataReader = new StreamReader(File.Open(ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //JsonSerializer serializer = new JsonSerializer();
            ///AdmConfig deserializedConfig = (AdmConfig)serializer.Deserialize(configReader, typeof(AdmConfig));
            var yamlDeserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            AdmConfig deserializedConfigYaml = yamlDeserializer.Deserialize<AdmConfig>(LoadFile(ConfigFilePath));
            Config = deserializedConfigYaml ?? Config;
            Loading = false;
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
