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
    "id", "cs", "de", "en", "es", "fr", "it", "hu", "nl", "nb",
    "fa", "pl", "pt-BR", "pt-PT", "ro", "sr", "vi", "tr", "el",
    "ru", "uk", "ja", "zh-Hans", "zh-Hant"
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
                var neutralLanguage = topLanguage.Split('-')[0]; // example: "fr"
                if (SupportedCultures.Contains(neutralLanguage))
                {
                    SelectedLanguageCode = neutralLanguage;
                }
                else
                {
                    SelectedLanguageCode = CultureInfo.CurrentUICulture.Name; // example: "fr-FR"
                    // SelectedLanguageCode = new CultureInfo("en-US");
                }
            }
            await _localSettingsService.SaveSettingAsync("SelectedLanguageCode", SelectedLanguageCode);
        }
        return SelectedLanguageCode;
    }
}

