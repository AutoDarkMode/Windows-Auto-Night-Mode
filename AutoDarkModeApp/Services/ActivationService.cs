using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;

namespace AutoDarkModeApp.Services;

public class ActivationService(ILocalSettingsService localSettingsService,INavigationService navigationService) : IActivationService
{
    public async Task ActivateAsync(object activationArgs)
    {
        // Navigate to default page
        navigationService.NavigateTo(typeof(TimeViewModel).FullName!);

        // Move window to config position
        await MoveWindowAsync();

        // Activate the MainWindow.
        App.MainWindow.Activate();
    }

    private async Task MoveWindowAsync()
    {
        var left = await localSettingsService.ReadSettingAsync<int>("X");
        var top = await localSettingsService.ReadSettingAsync<int>("Y");
        var width = await localSettingsService.ReadSettingAsync<int>("Width");
        var height = await localSettingsService.ReadSettingAsync<int>("Height");
        App.MainWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(left, top, width, height));
    }
}