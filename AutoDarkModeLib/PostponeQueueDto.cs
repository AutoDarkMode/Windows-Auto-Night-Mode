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
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeLib;

public class PostponeQueueDto
{
    public List<PostponeItemDto> Items { get; set; } = [];

    public PostponeQueueDto() { }

    public PostponeQueueDto(List<PostponeItemDto> items)
    {
        Items = items;
    }

    public static PostponeQueueDto Deserialize(string data)
    {
        var yamlDeserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
        PostponeQueueDto deserialized = yamlDeserializer.Deserialize<PostponeQueueDto>(data);
        return deserialized;
    }

    public string Serialize()
    {
        ISerializer yamlSerializer = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
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

    public LocalizedPostponeData GetLocalizationData()
    {
        var data = new LocalizedPostponeData
        {
            MainReasonKey = $"PostponeReason_{Reason}",
            PostponesUntilKey = "PostponeReason_PostponesUntil",
            PostponesUntilConditionKey = "PostponeReason_PostponesUntilCondition",
            UntilNextSunriseKey = "PostponeReason_UntilNextSunrise",
            UntilNextSunsetKey = "PostponeReason_UntilNextSunset",
            OriginalReason = Reason,
            SkipType = SkipType,
            Expires = Expires,
            Expiry = Expiry,
            Culture = Culture,
        };

        if (TranslatedReason == null || TranslatedReason == Reason)
        {
            string[] split = Regex.Split(Reason, @"(?<!^)(?=[A-Z])");
            data.DefaultReasonText = string.Join(" ", split);
        }
        else
        {
            data.DefaultReasonText = TranslatedReason;
        }

        return data;
    }
}

public class LocalizedPostponeData
{
    public string MainReasonKey { get; set; }
    public string PostponesUntilKey { get; set; }
    public string PostponesUntilConditionKey { get; set; }
    public string UntilNextSunriseKey { get; set; }
    public string UntilNextSunsetKey { get; set; }

    public string OriginalReason { get; set; }
    public string DefaultReasonText { get; set; }
    public SkipType SkipType { get; set; }
    public bool Expires { get; set; }
    public DateTime? Expiry { get; set; }
    public CultureInfo Culture { get; set; }

    public bool IsPauseAutoSwitchWithoutExpiry => OriginalReason == Helper.PostponeItemPauseAutoSwitch && !Expires;
}
