using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeConfig
{
    public class UpdateInfo
    {
        public string Tag { get; set; }
        public string FileName { get; set; }
        public bool AutoUpdateAvailable { get; set; }
        public string Message { get; set; }

        public static UpdateInfo Deserialize(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            UpdateInfo deserialized = yamlDeserializer.Deserialize<UpdateInfo>(data);
            return deserialized;
        }

        public string GetUpdateUrl(string url, bool custom = false)
        {
            if (!url.EndsWith("/"))
            {
                url = url += "/";
            }
            return custom ? url : $"{url}{Tag}/{FileName}.zip";
        }

        public string GetUpdateInfoPage()
        {
            string url = "https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/";
            return $"{url}{Tag}";
        }

        public string GetUpdateHashUrl(string url, bool custom = false)
        {
            if (!url.EndsWith("/"))
            {
                url = url += "/";
            }
            return custom ? url : $"{url}{Tag}/{FileName}.sha256";
        }
    }
}
