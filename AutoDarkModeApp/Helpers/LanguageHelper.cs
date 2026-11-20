using System.Globalization;
using Microsoft.Windows.Globalization;

namespace AutoDarkModeApp.Helpers;

public static class LanguageHelper
{
    // Must be a member of SupportedCultures, otherwise it is rejected on every launch and no entry
    // in the language dropdown can match it.
    public static string SelectedLanguageCode { get; set; } = "en";

    public static readonly string[] SupportedCultures =
    [
        // Left-to-Right (LTR) languages
        "cs", "de", "el", "en", "es", "fr", "hu", "id", "it", "ja", "ko", "nb",
        "nl", "pl", "pt-BR", "pt-PT", "ro", "ru", "sr", "sv", "tr", "uk",
        "vi", "zh-Hans", "zh-Hant",

        // Right-to-Left (RTL) languages
        "ar", "fa", "he"
    ];

    public static string GetDefaultLanguage()
    {
        var localSettings = App.GetService<ILocalSettingsService>();
        var savedLanguage = localSettings.GetValue<string>("SelectedLanguageCode");

        if (!string.IsNullOrEmpty(savedLanguage) && TryMatchSupportedLanguage(savedLanguage, out var saved))
        {
            SelectedLanguageCode = saved;
            return SelectedLanguageCode;
        }

        var preferredLanguages = ApplicationLanguages.Languages; // example: ["fr-FR", "en-US", "de-DE"]
        var topLanguage = preferredLanguages.Any()
            ? preferredLanguages[0]
            : CultureInfo.CurrentUICulture.Name; // very unlikely, but just in case

        if (TryMatchSupportedLanguage(topLanguage, out var matched))
        {
            SelectedLanguageCode = matched;
        }
        // else keep the default "en"

        localSettings.SetValue("SelectedLanguageCode", SelectedLanguageCode);
        return SelectedLanguageCode;
    }

    /// <summary>
    /// Resolves a language tag against <see cref="SupportedCultures"/>, dropping one subtag at a
    /// time: "fr-FR" matches "fr", "zh-Hans-CN" matches "zh-Hans". Returns the canonically cased
    /// entry so it lines up with the codes bound to the language dropdown.
    /// </summary>
    private static bool TryMatchSupportedLanguage(string languageTag, out string match)
    {
        for (var candidate = languageTag; !string.IsNullOrEmpty(candidate);)
        {
            var supported = SupportedCultures.FirstOrDefault(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(supported))
            {
                match = supported;
                return true;
            }

            var lastSeparatorIndex = candidate.LastIndexOf('-');
            if (lastSeparatorIndex < 0)
            {
                break;
            }
            candidate = candidate[..lastSeparatorIndex];
        }

        match = string.Empty;
        return false;
    }
}
