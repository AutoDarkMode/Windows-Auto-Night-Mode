using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Windowing;

namespace AutoDarkModeApp.Helpers;

public class AdaptingToLegacyConfigHelper
{
    public static void MigrationSettings(ILocalSettingsService localSettingsService)
    {
        var legacySettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoDarkMode", "ApplicationData", "LocalSettings.json");
        var legacySettingsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoDarkMode");

        if (!File.Exists(legacySettingsPath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(legacySettingsPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Legacy local settings must be a JSON object.");
            }

            var settings = document.RootElement;
            MigrateIntSetting(settings, localSettingsService, "X", "MainWindowPositionX");
            MigrateIntSetting(settings, localSettingsService, "Y", "MainWindowPositionY");
            MigrateIntSetting(settings, localSettingsService, "Width", "MainWindowWidth");
            MigrateIntSetting(settings, localSettingsService, "Height", "MainWindowHeight");
            MigrateWindowState(settings, localSettingsService);
            MigrateBooleanSetting(settings, localSettingsService, "NotFirstRun");
            MigrateBooleanSetting(settings, localSettingsService, "LanguageChanged");
            MigrateBooleanSetting(settings, localSettingsService, "TwelveHourClock");
            MigrateStringSetting(settings, localSettingsService, "SelectedLanguageCode");

            Directory.Delete(legacySettingsFolderPath, true);
        }
        catch (IOException exception)
        {
            Debug.WriteLine($"Failed to read legacy local settings: {exception}");
        }
        catch (JsonException exception)
        {
            Debug.WriteLine($"Failed to parse legacy local settings: {exception}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.WriteLine($"Access to legacy local settings was denied: {exception}");
        }
    }

    private static void MigrateIntSetting(JsonElement settings, ILocalSettingsService localSettingsService, string legacyKey, string newKey)
    {
        if (localSettingsService.LocalSettings.Values.ContainsKey(newKey) || !settings.TryGetProperty(legacyKey, out var serializedValue) || !TryGetInt32(serializedValue, out var value))
        {
            return;
        }

        localSettingsService.SetValue(newKey, value);
    }

    private static void MigrateWindowState(JsonElement settings, ILocalSettingsService localSettingsService)
    {
        const string newKey = "IsMainWindowMaximized";
        if (localSettingsService.LocalSettings.Values.ContainsKey(newKey) || !settings.TryGetProperty("WindowState", out var serializedValue) || !TryGetInt32(serializedValue, out var windowState))
        {
            return;
        }

        localSettingsService.SetValue(newKey, windowState == (int)OverlappedPresenterState.Maximized);
    }

    private static void MigrateBooleanSetting(JsonElement settings, ILocalSettingsService localSettingsService, string key)
    {
        if (localSettingsService.LocalSettings.Values.ContainsKey(key) || !settings.TryGetProperty(key, out var serializedValue) || !TryGetBoolean(serializedValue, out var value))
        {
            return;
        }

        localSettingsService.SetValue(key, value);
    }

    private static void MigrateStringSetting(JsonElement settings, ILocalSettingsService localSettingsService, string key)
    {
        if (localSettingsService.LocalSettings.Values.ContainsKey(key) || !settings.TryGetProperty(key, out var serializedValue) || !TryGetString(serializedValue, out var value))
        {
            return;
        }

        localSettingsService.SetValue(key, value);
    }

    private static bool TryGetInt32(JsonElement serializedValue, out int value)
    {
        switch (serializedValue.ValueKind)
        {
            case JsonValueKind.Number:
                return serializedValue.TryGetInt32(out value);
            case JsonValueKind.String:
                return int.TryParse(serializedValue.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            default:
                value = default;
                return false;
        }
    }

    private static bool TryGetBoolean(JsonElement serializedValue, out bool value)
    {
        switch (serializedValue.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.String:
                return bool.TryParse(serializedValue.GetString(), out value);
            default:
                value = default;
                return false;
        }
    }

    private static bool TryGetString(JsonElement serializedValue, out string value)
    {
        value = string.Empty;
        if (serializedValue.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        value = serializedValue.GetString() ?? string.Empty;
        if (value.Length > 1 && value[0] == '"' && value[^1] == '"')
        {
            try
            {
                value = JsonSerializer.Deserialize<string>(value) ?? string.Empty;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return !string.IsNullOrWhiteSpace(value);
    }
}
