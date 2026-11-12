using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.UI.WindowManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_LAYOUTRTL = 0x00400000;

    public MainWindow(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();

        if (Content is FrameworkElement rootElement)
        {
            rootElement.FlowDirection = LanguageHelper.FlowDirection;
        }

        Title = Debugger.IsAttached ? "Auto Dark Mode Debug" : "Auto Dark Mode";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        TitleBar.Subtitle = Debugger.IsAttached ? "Debug" : "";

        // Listen for theme changes on the title bar and clean up when window closes
        TitleBar.ActualThemeChanged += ApplySystemThemeToCaptionButtons;
        Closed += (s, e) => TitleBar.ActualThemeChanged -= ApplySystemThemeToCaptionButtons;

        // Initial apply
        ApplySystemThemeToCaptionButtons(TitleBar, new object());

        // Set app icons only if present to avoid throwing on missing files
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AutoDarkModeIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
            AppWindow.SetTaskbarIcon(iconPath);
            AppWindow.SetTitleBarIcon(iconPath);
        }

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        if (LanguageHelper.IsRightToLeft)
        {
            // Mirror the native window so the caption buttons (minimize, maximize, close) move to the left.
            var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_LAYOUTRTL));

            // The title bar's back arrow glyph is not mirrored automatically; flip it so it points right.
            TitleBar.Loaded += (_, _) => MirrorTitleBarBackButton();
        }
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

    private void MirrorTitleBarBackButton()
    {
        var backButton = FindDescendant<Button>(TitleBar, b => b.Name.Contains("Back", StringComparison.OrdinalIgnoreCase));
        if (backButton is null)
        {
            return;
        }

        backButton.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        backButton.RenderTransform = new Microsoft.UI.Xaml.Media.ScaleTransform { ScaleX = -1 };
    }

    private static T? FindDescendant<T>(DependencyObject parent, Func<T, bool> predicate) where T : FrameworkElement
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match && predicate(match))
            {
                return match;
            }

            var result = FindDescendant(child, predicate);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
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

    private void ApplySystemThemeToCaptionButtons(FrameworkElement sender, object args)
    {
        // Align title bar and caption button colors with WinUI / Windows 11 semantics
        // Follow issue https://github.com/microsoft/microsoft-ui-xaml/issues/9722
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
