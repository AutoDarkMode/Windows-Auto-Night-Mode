using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;

namespace AutoDarkModeApp.Services;

public class ActivationService : IActivationService
{
    private readonly INavigationService _navigationService;
    private readonly ILocalSettingsService _localSettings;

    public ActivationService(INavigationService navigationService, ILocalSettingsService localSettingsService)
    {
        _navigationService = navigationService;
        _localSettings = localSettingsService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Navigate to default page
        _navigationService.NavigateTo(typeof(TimeViewModel).FullName!);

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