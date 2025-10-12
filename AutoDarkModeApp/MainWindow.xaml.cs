using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.WindowManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    public MainWindow()
    {
        _navigationService = App.GetService<INavigationService>();
        InitializeComponent();

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        TitleBar.Subtitle = Debugger.IsAttached ? "Debug" : "";
        TitleBar.ActualThemeChanged += (s, e) => ApplySystemThemeToCaptionButtons();

        ApplySystemThemeToCaptionButtons();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTitleBarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));

        Title = Debugger.IsAttached ? "Auto Dark Mode Debug" : "Auto Dark Mode";

        // TODO: No one knows what the correct way to use it is. Waiting for official examples.
        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow([In] IntPtr hwnd);
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpiForWindow = GetDpiForWindow(hWnd);
        double scaleFactor = dpiForWindow / 96.0;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(870 * scaleFactor);
            presenter.PreferredMinimumHeight = (int)(600 * scaleFactor);
            presenter.PreferredMaximumHeight = 10000;
            presenter.PreferredMaximumWidth = 10000;
        }

        _navigationService.Frame = NavigationFrame;
        _navigationService.InitializeNavigationView(NavigationViewControl);

        _navigationService.InitializeBreadcrumbBar(BreadcrumBarControl);

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

    private void ApplySystemThemeToCaptionButtons()
    {
        var backgroundHoverColor = TitleBar.ActualTheme == ElementTheme.Dark ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
        AppWindow.TitleBar.ButtonHoverBackgroundColor = backgroundHoverColor;
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
