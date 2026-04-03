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

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    public MainWindow(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();

        Title = Debugger.IsAttached ? "Auto Dark Mode Debug" : "Auto Dark Mode";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        TitleBar.Subtitle = Debugger.IsAttached ? "Debug" : "";
        TitleBar.ActualThemeChanged += (s, e) => ApplySystemThemeToCaptionButtons();

        ApplySystemThemeToCaptionButtons();

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico");
        AppWindow.SetIcon(iconPath);
        AppWindow.SetTaskbarIcon(iconPath);
        AppWindow.SetTitleBarIcon(iconPath);

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        double scaleFactor = dpi / 96.0;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(860 * scaleFactor);
            presenter.PreferredMinimumHeight = (int)(600 * scaleFactor);
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
