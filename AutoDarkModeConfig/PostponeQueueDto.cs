using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeConfig
{
    public class PostponeQueueDto
    {
        public List<PostponeItemDto> Items { get; set; }

        public PostponeQueueDto() { }

        public PostponeQueueDto(List<PostponeItemDto> items)
        {
            this.Items = items;
        }

        public static PostponeQueueDto Deserialize(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            PostponeQueueDto deserialized = yamlDeserializer.Deserialize<PostponeQueueDto>(data);
            return deserialized;
        }

        public string Serialize()
        {
            YamlDotNet.Serialization.ISerializer yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            return yamlSerializer.Serialize(this);
        }
    }

    public class PostponeItemDto
    {
        public string Reason { get; set; }
        public DateTime? Expiry { get; set; }
        public PostponeItemDto() { }

        public PostponeItemDto(string reason, DateTime? expiry = null)
        {
            Reason = reason;
            Expiry = expiry;
        }

        public override string ToString()
        {
            string[] split = Regex.Split(Reason, @"(?<!^)(?=[A-Z])");
            Reason = string.Join(" ", split);

            if (Expiry != null) return $"{Reason} postpones until {Expiry:HH:mm}";
            return $"{Reason} postpones until its condition is met";
        }
    }
}
