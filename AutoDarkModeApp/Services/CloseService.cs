using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI.Windowing;

namespace AutoDarkModeApp.Services;

public class CloseService(ILocalSettingsService localSettingsService) : ICloseService
{
    public async Task CloseAsync()
    {
        if (App.MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var position = App.MainWindow.AppWindow.Position;
            var size = App.MainWindow.AppWindow.Size;

            await localSettingsService.SaveSettingAsync("WindowState", (int)presenter.State);

            if (presenter.State == OverlappedPresenterState.Restored)
            {
                await localSettingsService.SaveSettingAsync("X", position.X);
                await localSettingsService.SaveSettingAsync("Y", position.Y);
                await localSettingsService.SaveSettingAsync("Width", size.Width);
                await localSettingsService.SaveSettingAsync("Height", size.Height);
            }
        }
    }
}
