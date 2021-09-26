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
    }
}
