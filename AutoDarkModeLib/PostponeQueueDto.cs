using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using Resources = AutoDarkModeLib.Properties.Resources;

namespace AutoDarkModeLib
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
        public string TranslatedReason { get; set; }
        public DateTime? Expiry { get; set; }
        public PostponeItemDto() { }

        public PostponeItemDto(string reason, DateTime? expiry = null)
        {
            Reason = reason;
            Expiry = expiry;
        }

        public override string ToString()
        {
            if (TranslatedReason != null && (TranslatedReason.Length > 0 || TranslatedReason == Reason))
            {
                string[] split = Regex.Split(Reason, @"(?<!^)(?=[A-Z])");
                TranslatedReason = string.Join(" ", split);
            }

            if (Expiry != null) return $"{TranslatedReason} {Resources.PostponeReasonPostponesUntil} {Expiry:HH:mm}";
            else if (Reason == Helper.SkipSwitchPostponeItemName) return $"{TranslatedReason} {Resources.PostponeReasonPostponesUntilNextSwitch}";
            return $"{TranslatedReason} {Resources.PostponeReasonPostponesUntilCondition}";
        }
    }
}
