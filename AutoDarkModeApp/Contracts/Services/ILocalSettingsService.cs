using Microsoft.Windows.Storage;

namespace AutoDarkModeApp.Contracts.Services;

public interface ILocalSettingsService
{
    ApplicationDataContainer LocalSettings { get; }
    T? GetValue<T>(string key, T? defaultValue = default);
    void SetValue<T>(string key, T value);
}
