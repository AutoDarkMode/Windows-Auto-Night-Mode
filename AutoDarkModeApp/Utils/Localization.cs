namespace AutoDarkModeApp.Utils;

public class Localization
{
    public static string LanguageTranscoding(string language)
    {
        return language switch
        {
            "Česky (Czech)" => "cs",
            "Deutsch (German)" => "de",
            "Nederlands (Dutch)" => "nl",
            "English (English)" => "en-us",
            "Français (French)" => "fr",
            "Ελληνικά (Greek)" => "el",
            "Magyarul (Hungarian)" => "hu",
            "Bahasa Indonesia (Indonesian)" => "id",
            "Italiano (Italian)" => "it",
            "日本語 (Japanese)" => "ja",
            "Norwegian Bokmål" => "nb-no",
            "Persian (Farsi)" => "fa",
            "Polski (Polish)" => "pl",
            "Português (Portuguese)" => "pt-pt",
            "Português (Brazil)" => "pt-br",
            "Română (Romanian)" => "ro",
            "Русский (Russian)" => "ru",
            "Srpski (Serbian)" => "sr",
            "Español (Spanish)" => "es",
            "Türkçe (Turkish)" => "tr",
            "Українська (Ukrainian)" => "uk",
            "Tiếng Việt (Vietnamese)" => "vi",
            "简体中文 (Simplified Chinese)" => "zh-hans",
            "繁體中文 (Traditional Chinese)" => "zh-hant",
            _ => "en-us",
        };
    }
}
