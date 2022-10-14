using System;
using System.Collections.Generic;
using System.Globalization;
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
        public bool Expires { get; set; }
        public SkipType SkipType { get; set; }
        private CultureInfo Culture { get; set; }
        public bool IsUserClearable { get; set; }
        public PostponeItemDto() { }
        public PostponeItemDto(string reason, DateTime? expiry = null, bool expires = false, SkipType skipType = SkipType.Unspecified, bool isUserClearable = false)
        {
            Reason = reason;
            Expiry = expiry;
            Expires = expires;
            SkipType = skipType;
            IsUserClearable = isUserClearable;
        }

        public void SetCulture(CultureInfo info)
        {
            Culture = info;
        }

        public override string ToString()
        {
            if (TranslatedReason != null && (TranslatedReason.Length == 0 || TranslatedReason == Reason))
            {
                string[] split = Regex.Split(Reason, @"(?<!^)(?=[A-Z])");
                TranslatedReason = string.Join(" ", split);
            }

            string postponeReasonPostponesUntil = Resources.ResourceManager.GetString("PostponeReasonPostponesUntil", Culture);
            string postponeReasonPostponesUntilNextSwitch = Resources.ResourceManager.GetString("PostponeReasonPostponesUntilNextSwitch", Culture);
            string postponeReasonPostponesUntilCondition = Resources.ResourceManager.GetString("PostponeReasonPostponesUntilCondition", Culture);

            if (Reason == Helper.PostponeItemPauseAutoSwitch && !Expires)
            {
                string pausedUntilNextSunrise = Resources.ResourceManager.GetString("PostponeReasonUntilNextSunset", Culture);
                string pausedUntilNextSunset = Resources.ResourceManager.GetString("PostponeReasonUntilNextSunrise", Culture);

                if (SkipType == SkipType.Sunrise)
                {

                    postponeReasonPostponesUntilNextSwitch = $"{pausedUntilNextSunrise}";
                }
                else if (SkipType == SkipType.Sunset)
                {
                    postponeReasonPostponesUntilNextSwitch = $"{pausedUntilNextSunset}";
                }
            }

            if (Expires) {
                if (Expiry.Value.Day > DateTime.Now.Day) return $"{TranslatedReason} {postponeReasonPostponesUntil} {Expiry:dddd HH:mm}";
                else return $"{TranslatedReason} {postponeReasonPostponesUntil} {Expiry:HH:mm}"; 
            }
            else if (Reason == Helper.PostponeItemPauseAutoSwitch) return $"{TranslatedReason} {postponeReasonPostponesUntilNextSwitch}";
            return $"{TranslatedReason} {postponeReasonPostponesUntilCondition}";
        }
    }
}
