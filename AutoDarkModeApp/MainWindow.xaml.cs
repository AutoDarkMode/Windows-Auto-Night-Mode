using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    public MainWindow(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();

        // TODO: No one knows what the correct way to use it is. Waiting for official examples.
        var overlappedPresenter = OverlappedPresenter.Create();
        overlappedPresenter.PreferredMinimumWidth = 680;
        overlappedPresenter.PreferredMinimumHeight = 320;
        AppWindow.SetPresenter(overlappedPresenter);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTitleBarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));

        Title = Debugger.IsAttached ? "Auto Dark Mode Debug" : "Auto Dark Mode";

        _navigationService.Frame = NavigationFrame;
        _navigationService.InitializeNavigationView(NavigationViewControl);

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
#if DEBUG
        TitleBar.Subtitle = "Debug";
#endif

        Closed += MainWindow_Closed;
    }

    private void NavViewTitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
        }
    }

    private void NavViewTitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        var postion = App.MainWindow.AppWindow.Position;
        var size = App.MainWindow.AppWindow.Size;
        var localSettings = App.GetService<ILocalSettingsService>();
        await Task.Run(async () =>
        {
            await localSettings.SaveSettingAsync("X", postion.X);
            await localSettings.SaveSettingAsync("Y", postion.Y);
            await localSettings.SaveSettingAsync("Width", size.Width);
            await localSettings.SaveSettingAsync("Height", size.Height);

            //TODO: MapLocationFinder will make WinUI app hang on exit, more information on https://github.com/microsoft/microsoft-ui-xaml/issues/10229
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
