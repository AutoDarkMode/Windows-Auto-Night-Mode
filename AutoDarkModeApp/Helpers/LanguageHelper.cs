using System.Globalization;
using System.Reflection;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.Windows.Globalization;

namespace AutoDarkModeApp.Helpers;

public static class LanguageHelper
{
    public static string SelectedLanguageCode { get; set; } = "en-US"; // equal to <DefaultLanguage>

    private static readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>()!;
    public static readonly string[] SupportedCultures =
    [
        // Left-to-Right (LTR) languages
        "cs", "de", "el", "en", "es", "fr", "hu", "id", "it", "ja", "ko", "nb",
        "nl", "pl", "pt-BR", "pt-PT", "ro", "ru", "sr", "sv", "tr", "uk",
        "vi", "zh-Hans", "zh-Hant",

        // Right-to-Left (RTL) languages
        "ar", "fa", "he"
    ];

    public static async Task<string> GetDefaultLanguageAsync()
    {
        var language = await _localSettingsService.ReadSettingAsync<string>("SelectedLanguageCode");
        if (!string.IsNullOrEmpty(language) && SupportedCultures.Contains(language))
        {
            SelectedLanguageCode = language;
        }
        else
        {
            var preferredLanguages = ApplicationLanguages.Languages; // example: ["fr-FR", "en-US", "de-DE"]
            string topLanguage;
            if (preferredLanguages.Any())
            {
                topLanguage = preferredLanguages[0];
            }
            else // very unlikely, but just in case
            {
                topLanguage = CultureInfo.CurrentUICulture.Name;
            }

            if (SupportedCultures.Contains(topLanguage))
            {
                SelectedLanguageCode = topLanguage;
            }
            else
            {
                var topLanguageArray = topLanguage.Split('-');
                //var neutralLanguage = topLanguageArray[0]; // example: "fr"
                var neutralLanguage = string.Join("-", topLanguageArray[..^1]); // example: "fr" or "zh-Hans"
                if (SupportedCultures.Contains(neutralLanguage))
                {
                    SelectedLanguageCode = neutralLanguage;
                }
                // else keep the default "en-US"
            }
            await _localSettingsService.SaveSettingAsync("SelectedLanguageCode", SelectedLanguageCode);
        }
        return SelectedLanguageCode;
    }
}
