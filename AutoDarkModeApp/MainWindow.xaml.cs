using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.System;
using Microsoft.UI.Windowing;
using Windows.UI.WindowManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly ThemeSettings themeSettings;

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

        // Listen for theme changes on the title bar and clean up when window closes
        themeSettings = ThemeSettings.CreateForWindowId(AppWindow.Id);
        themeSettings.Changed += ApplySystemThemeToCaptionButtons;
        Closed += (s, e) => themeSettings.Changed -= ApplySystemThemeToCaptionButtons;

        // Initial apply
        ApplySystemThemeToCaptionButtons(themeSettings, new object());

        // Set app icons only if present to avoid throwing on missing files
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AutoDarkModeIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
            AppWindow.SetTaskbarIcon(iconPath);
            AppWindow.SetTitleBarIcon(iconPath);
        }

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

    private void NavViewTitleBar_BackRequested(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
        }
    }

    private void NavViewTitleBar_PaneToggleRequested(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
    }

    private void ApplySystemThemeToCaptionButtons(ThemeSettings sender, object args)
    {
        // Align title bar and caption button colors with WinUI / Windows 11 semantics
        // Follow issue https://github.com/microsoft/microsoft-ui-xaml/issues/9722
        if (themeSettings.HighContrast)
        {
            AppWindow.TitleBar.ButtonBackgroundColor = null;
            AppWindow.TitleBar.ButtonForegroundColor = null;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = null;
            AppWindow.TitleBar.ButtonInactiveForegroundColor = null;
            AppWindow.TitleBar.ButtonHoverBackgroundColor = null;
            AppWindow.TitleBar.ButtonHoverForegroundColor = null;
            AppWindow.TitleBar.ButtonPressedBackgroundColor = null;
            AppWindow.TitleBar.ButtonPressedForegroundColor = null;
        }
        else
        {
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonForegroundColor = ThemeColorHelper.GetThemeColor("SystemControlPageTextBaseHighBrush");
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveForegroundColor = ThemeColorHelper.GetThemeColor("SystemControlForegroundChromeDisabledLowBrush");
            AppWindow.TitleBar.ButtonHoverBackgroundColor = ThemeColorHelper.GetThemeColor("SystemControlBackgroundListLowBrush");
            AppWindow.TitleBar.ButtonHoverForegroundColor = ThemeColorHelper.GetThemeColor("SystemControlForegroundBaseHighBrush");
            AppWindow.TitleBar.ButtonPressedBackgroundColor = ThemeColorHelper.GetThemeColor("SystemControlBackgroundListMediumBrush");
            AppWindow.TitleBar.ButtonPressedForegroundColor = ThemeColorHelper.GetThemeColor("SystemControlForegroundBaseHighBrush");
        }

    }

}
