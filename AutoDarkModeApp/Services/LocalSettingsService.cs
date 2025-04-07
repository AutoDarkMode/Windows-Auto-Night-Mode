using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Models;

using Microsoft.Extensions.Options;

namespace AutoDarkModeApp.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "AutoDarkMode/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        await InitializeAsync();
        if (_settings != null && _settings.TryGetValue(key, out var obj))
        {
            return await Json.ToObjectAsync<T>(obj.ToString()!);
        }
        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        await InitializeAsync();
        _settings[key] = await Json.StringifyAsync(value);
        await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
    }
}
