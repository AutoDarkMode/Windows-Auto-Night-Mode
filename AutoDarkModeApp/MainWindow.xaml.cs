using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.WindowManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    public MainWindow(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();

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
}
