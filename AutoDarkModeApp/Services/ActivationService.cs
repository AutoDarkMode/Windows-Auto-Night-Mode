using AutoDarkModeApp.Contracts.Services;

namespace AutoDarkModeApp.Services;

public class ActivationService : IActivationService
{
    private readonly ILocalSettingsService _localSettings;

    public ActivationService(ILocalSettingsService localSettingsService)
    {
        _localSettings = localSettingsService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Move window to config position
        await MoveWindowAsync();

        // Activate the MainWindow.
        App.MainWindow.Activate();
    }

    private async Task MoveWindowAsync()
    {
        var left = await _localSettings.ReadSettingAsync<int>("X");
        var top = await _localSettings.ReadSettingAsync<int>("Y");
        var width = await _localSettings.ReadSettingAsync<int>("Width");
        var height = await _localSettings.ReadSettingAsync<int>("Height");
        App.MainWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(left, top, width, height));
    }
}