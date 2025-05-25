using AutoDarkModeApp.Helpers;
using AutoDarkModeLib;

namespace AutoDarkModeApp.Utils;

public static class PostponeLocalizationExtensions
{
    public static string BuildLocalizedString(this LocalizedPostponeData data)
    {
        string reason = data.MainReasonKey.GetLocalized() ?? data.DefaultReasonText;
        string postponesUntil = data.PostponesUntilKey.GetLocalized();
        string postponesCondition = data.PostponesUntilConditionKey.GetLocalized();

        if (data.IsPauseAutoSwitchWithoutExpiry)
        {
            string sunriseText = data.UntilNextSunriseKey.GetLocalized();
            string sunsetText = data.UntilNextSunsetKey.GetLocalized();

            postponesUntil = data.SkipType switch
            {
                SkipType.UntilSunset => sunsetText,
                SkipType.UntilSunrise => sunriseText,
                _ => postponesUntil,
            };
        }

        if (data.Expires && data.Expiry.HasValue)
        {
            string timeFormat = data.Expiry.Value.Day > DateTime.Now.Day ? "dddd HH:mm" : "HH:mm";
            return $"{reason} {postponesUntil} {data.Expiry.Value.ToString(timeFormat, data.Culture)}";
        }
        else if (data.IsPauseAutoSwitchWithoutExpiry)
        {
            return $"{reason} {postponesUntil}";
        }

        return $"{reason} {postponesCondition}";
    }
}
