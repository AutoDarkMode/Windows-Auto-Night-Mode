using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeConfig
{
    public class UpdateInfo
    {
        public string Tag { get; set; }
        public string FileName { get; set; }
        public bool AutoUpdateAvailable { get; set; }

        public static UpdateInfo Deserialize(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            UpdateInfo deserialized = yamlDeserializer.Deserialize<UpdateInfo>(data);
            return deserialized;
        }

        public string GetUpdateUrl()
        {
            //return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{Tag}/{FileName}.zip";
            return "https://cloud.walzen.org/s/SKpoHfYfRW9tQ2W/download/AdmUpdateTest.zip";
        }

        public string GetUpdateInfoPage()
        {
            return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{Tag}";
        }

        public string GetUpdateHashUrl()
        {
            //return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{Tag}/{FileName}.sha256";
            return "https://cloud.walzen.org/s/d5n8xxka27w9K5B/download/AdmUpdateTest.sha256";
        }
    }
}
