using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeConfig
{
    public class UpdateInfo
    {
        public const string baseUrl = "https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/";
        public string Tag { get; set; }
        public string FileName { get; set; }
        public bool AutoUpdateAvailable { get; set; }

        public static UpdateInfo Deserialize(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            UpdateInfo deserialized = yamlDeserializer.Deserialize<UpdateInfo>(data);
            return deserialized;
        }

        public string GetUpdateUrl(string url = baseUrl)
        {
            if (url != baseUrl)
            {
                return url;
            }
            return $"{url}{Tag}/{FileName}.zip";
        }

        public string GetUpdateInfoPage(string url = baseUrl)
        {
            return $"{url}{Tag}";
        }

        public string GetUpdateHashUrl(string url = baseUrl)
        {
            if (url != baseUrl)
            {
                return url;
            }
            return $"{url}{Tag}/{FileName}.sha256";
        }
    }
}
