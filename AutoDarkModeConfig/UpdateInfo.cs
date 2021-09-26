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
            return "https://cloud.walzen.org/s/M3y8BTc9xM6gYac/download/AdmUpdateTest.zip";
        }

        public string GetUpdateInfoPage()
        {
            return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{Tag}";
        }

        public string GetUpdateHashUrl()
        {
            //return $"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/{Tag}/{FileName}.sha256";
            return "https://cloud.walzen.org/s/S6f2C5Ra5DMgSNZ/download/AdmUpdateTest.sha256";
        }
    }
}
