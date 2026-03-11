using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI.Windowing;

namespace AutoDarkModeApp.Services;

public class CloseService(ILocalSettingsService localSettingsService) : ICloseService
{
    public async Task CloseAsync()
    {
        var presenter = App.MainWindow.AppWindow.Presenter as OverlappedPresenter;
        var position = App.MainWindow.AppWindow.Position;
        var size = App.MainWindow.AppWindow.Size;
        await Task.Run(async () =>
        {
            if (presenter is not null)
            {
                await localSettingsService.SaveSettingAsync("WindowState", (int)presenter.State);

                if (presenter.State == OverlappedPresenterState.Restored)
                {
                    await localSettingsService.SaveSettingAsync("X", position.X);
                    await localSettingsService.SaveSettingAsync("Y", position.Y);
                    await localSettingsService.SaveSettingAsync("Width", size.Width);
                    await localSettingsService.SaveSettingAsync("Height", size.Height);
                }
            }

            //NOTE: MapLocationFinder will make WinUI app hang on exit, more information on https://github.com/microsoft/microsoft-ui-xaml/issues/10229
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        });
    }
}
