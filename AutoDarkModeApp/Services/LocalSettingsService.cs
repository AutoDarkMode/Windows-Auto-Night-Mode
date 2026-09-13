using Microsoft.Windows.Storage;

namespace AutoDarkModeApp.Services;

public partial class LocalSettingsService : ILocalSettingsService, IDisposable
{
    public ApplicationDataContainer LocalSettings => _applicationData.LocalSettings;

    private readonly ApplicationData _applicationData;

    private bool _disposed;

    public LocalSettingsService()
    {
        _applicationData = ApplicationData.GetForUnpackaged("AutoDarkMode", "Windows-Auto-Night-Mode");
    }

    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        return LocalSettings.Values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        LocalSettings.Values[key] = value;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _applicationData.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
