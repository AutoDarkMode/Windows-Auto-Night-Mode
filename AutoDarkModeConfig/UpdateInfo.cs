using System.Globalization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeConfig
{
    public class UpdateInfo
    {
        public const string changelogUrl = "https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/";
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

        public string Serialize()
        {
            YamlDotNet.Serialization.ISerializer yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            return yamlSerializer.Serialize(this);
        }

        public string GetUpdateUrl(string url, bool custom = false)
        {
            if (custom)
            {
                return url;
            }
            else
            {
                string fileUrl = string.Format(CultureInfo.InvariantCulture, url, Tag, $"{FileName}.zip");
                return fileUrl;
            }
        }

        public string GetUpdateInfoPage()
        {
            string url = changelogUrl;
            return $"{url}{Tag}";
        }

        public string GetUpdateHashUrl(string url, bool custom = false)
        {
            if (custom)
            {
                return url;
            }
            else
            {
                string hashUrl = string.Format(CultureInfo.InvariantCulture, url, Tag, $"{FileName}.sha256");
                return hashUrl;
            }
        }
    }
}
