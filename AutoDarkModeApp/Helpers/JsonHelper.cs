using System.Text.Encodings.Web;
using System.Text.Json;

namespace AutoDarkModeApp.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return await Task.Run(() =>
            JsonSerializer.Deserialize<T>(value, _options));
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run(() =>
            JsonSerializer.Serialize(value, _options));
    }
}