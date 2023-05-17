#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Resources = AutoDarkModeLib.Properties.Resources;

namespace AutoDarkModeLib
{
    public class PostponeQueueDto
    {
        public List<PostponeItemDto> Items { get; set; } = new();

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
        [YamlIgnore]
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

                if (SkipType == SkipType.UntilSunset)
                {

                    postponeReasonPostponesUntilNextSwitch = $"{pausedUntilNextSunrise}";
                }
                else if (SkipType == SkipType.UntilSunrise)
                {
                    postponeReasonPostponesUntilNextSwitch = $"{pausedUntilNextSunset}";
                }
            }

            if (Expires)
            {
                if (Expiry.HasValue && Expiry.Value.Day > DateTime.Now.Day) return $"{TranslatedReason} {postponeReasonPostponesUntil} {Expiry.Value.ToString("dddd HH:mm", Culture)}";
                else return $"{TranslatedReason} {postponeReasonPostponesUntil} {Expiry:HH:mm}";
            }
            else if (Reason == Helper.PostponeItemPauseAutoSwitch) return $"{TranslatedReason} {postponeReasonPostponesUntilNextSwitch}";
            return $"{TranslatedReason} {postponeReasonPostponesUntilCondition}";
        }
    }
}
