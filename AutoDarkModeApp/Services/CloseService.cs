using Microsoft.UI.Windowing;

namespace AutoDarkModeApp.Services;

public class CloseService(ILocalSettingsService localSettingsService) : ICloseService
{
    public void Close()
    {
        if (App.MainWindow.AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        localSettingsService.SetValue("IsMainWindowMaximized", presenter.State == OverlappedPresenterState.Maximized);

        if (presenter.State == OverlappedPresenterState.Restored)
        {
            var position = App.MainWindow.AppWindow.Position;
            var size = App.MainWindow.AppWindow.Size;

            localSettingsService.SetValue("MainWindowPositionX", position.X);
            localSettingsService.SetValue("MainWindowPositionY", position.Y);
            localSettingsService.SetValue("MainWindowWidth", size.Width);
            localSettingsService.SetValue("MainWindowHeight", size.Height);
        }
    }
}
