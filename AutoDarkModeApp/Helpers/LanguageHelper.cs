using System.Globalization;
using Microsoft.Windows.Globalization;

namespace AutoDarkModeApp.Helpers;

public static class LanguageHelper
{
    // Must be a member of SupportedCultures, otherwise it is rejected on every launch and no entry
    // in the language dropdown can match it.
    public static string? SelectedLanguageCode { get; set; } = "en";

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

        if (!string.IsNullOrEmpty(savedLanguage) && TryMatchSupportedCulture(savedLanguage, out var saved))
        {
            SelectedLanguageCode = saved;
            return SelectedLanguageCode;
        }

        var preferredLanguages = ApplicationLanguages.Languages; // example: ["fr-FR", "en-US", "de-DE"]
        var topLanguage = preferredLanguages.Any()
            ? preferredLanguages[0]
            : CultureInfo.CurrentUICulture.Name; // very unlikely, but just in case

        if (TryMatchSupportedCulture(topLanguage, out var matched))
        {
            SelectedLanguageCode = matched;
        }
        // else keep the default

        localSettings.SetValue("SelectedLanguageCode", SelectedLanguageCode);

        if (SelectedLanguageCode is null)
        {
            return "en";
        }
        return SelectedLanguageCode;
    }

    /// <summary>
    /// Resolves a language tag against <see cref="SupportedCultures"/>, dropping one subtag at a
    /// time: "fr-FR" matches "fr", "zh-Hans-CN" matches "zh-Hans". Returns the canonically cased
    /// entry so it lines up with the codes bound to the language dropdown.
    /// </summary>
    private static bool TryMatchSupportedCulture(string languageTag, out string match)
    {
        for (var candidate = languageTag; !string.IsNullOrEmpty(candidate);)
        {
            var supported = SupportedCultures.FirstOrDefault(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase));
            if (supported != null)
            {
                match = supported;
                return true;
            }

            var lastSeparator = candidate.LastIndexOf('-');
            if (lastSeparator < 0)
            {
                break;
            }
            candidate = candidate[..lastSeparator];
        }

        match = string.Empty;
        return false;
    }
}
