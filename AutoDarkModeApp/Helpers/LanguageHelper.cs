using System.Globalization;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.Windows.Globalization;

namespace AutoDarkModeApp.Helpers;

public static class LanguageHelper
{
    private static string? _selectedLanguageCode;
    public static string? SelectedLanguageCode
    {
        get => _selectedLanguageCode;
        set => _selectedLanguageCode = value;
    }

    private static readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>()!;
    public static readonly string[] SupportedCultures =
    [
        // Left-to-Right (LTR) languages
        "cs",
        "de",
        "en",
        "es",
        "fr",
        "hu",
        "id",
        "it",
        "ja",
        "nb",
        "nl",
        "pl",
        "pt-BR",
        "pt-PT",
        "ro",
        "ru",
        "sr",
        "tr",
        "uk",
        "vi",
        "zh-Hans",
        "zh-Hant",
        // Right-to-Left (RTL) languages
        "ar",
        "fa",
        "he",
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
                var neutralLanguage = topLanguageArray[0]; // example: "fr"
                if (topLanguageArray.Length > 2)
                {
                    neutralLanguage += "-" + topLanguageArray[1];
                }
                if (SupportedCultures.Contains(neutralLanguage))
                {
                    SelectedLanguageCode = neutralLanguage;
                }
                else
                {
                    SelectedLanguageCode = CultureInfo.CurrentUICulture.Name; // example: "fr-FR"
                }
            }
            await _localSettingsService.SaveSettingAsync("SelectedLanguageCode", SelectedLanguageCode);
        }
        return SelectedLanguageCode;
    }
}
