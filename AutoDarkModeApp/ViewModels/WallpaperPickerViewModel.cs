using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class WallpaperPickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    public enum WallpaperDisplayMode
    {
        Picture,
        PictureMM,
        SolidColor,
        Spotlight,
    }

    public enum WallpaperDisplayFlags
    {
        None = 0,
        ShowImageSettings = 1,
        ShowMonitorSettings = 2,
        ShowFillingWaySettings = 4,
        ShowColorSettings = 8,
        ShowSpotlight = 16,

        // Predefined combinations
        PictureMode = ShowImageSettings | ShowFillingWaySettings,
        PictureMMMode = ShowImageSettings | ShowMonitorSettings | ShowFillingWaySettings,
        SolidColorMode = ShowColorSettings,
        SpotlightMode = ShowSpotlight,
    }

    public enum WallpaperFillingMode
    {
        Center,
        Stretch,
        Fit,
        Fill,
    }

    [ObservableProperty]
    public partial bool IsWallpaperSwitchEnabled { get; set; }

    [ObservableProperty]
    public partial ApplicationTheme SelectWallpaperThemeMode { get; set; }

    [ObservableProperty]
    public partial WallpaperDisplayMode CurrentDisplayMode { get; set; }

    [ObservableProperty]
    public partial WallpaperDisplayFlags CurrentDisplayFlags { get; set; }

    [ObservableProperty]
    public partial ImageSource? GlobalWallpaperSource { get; set; }

    [ObservableProperty]
    public partial string? GlobalWallpaperPath { get; set; }

    [ObservableProperty]
    public partial object? SelectMonitor { get; set; }

    [ObservableProperty]
    public partial WallpaperFillingMode SelectWallpaperFillingMode { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? ColorPreviewBorderBackground { get; set; }

    public ICommand PickFileCommand { get; }

    public WallpaperPickerViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        SelectWallpaperThemeMode = Application.Current.RequestedTheme;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();

        PickFileCommand = new RelayCommand(async () =>
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker()
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
                FileTypeFilter =
                {
                    ".jpg",
                    ".jpeg",
                    ".bmp",
                    ".dib",
                    ".png",
                    ".jff",
                    ".jpe",
                    ".gif",
                    ".tif",
                    ".tiff",
                    ".wdp",
                    ".heic",
                    ".heif",
                    ".heics",
                    ".heifs",
                    ".hif",
                    ".avci",
                    ".avcs",
                    ".avif",
                    ".avifs",
                    ".jxr",
                    ".jxl",
                },
            };
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                GlobalWallpaperPath = file.Path;
                GlobalWallpaperSource = new BitmapImage(new Uri(GlobalWallpaperPath));
            }
        });
    }

    private void LoadSettings()
    {
        IsWallpaperSwitchEnabled = _builder.Config.WallpaperSwitch.Enabled;

        SelectWallpaperFillingMode = _builder.Config.WallpaperSwitch.Component.Position switch
        {
            WallpaperPosition.Fill => WallpaperFillingMode.Fill,
            WallpaperPosition.Fit => WallpaperFillingMode.Fit,
            WallpaperPosition.Stretch => WallpaperFillingMode.Stretch,
            WallpaperPosition.Center => WallpaperFillingMode.Center,
            _ => WallpaperFillingMode.Fill,
        };

        CurrentDisplayFlags = WallpaperDisplayFlags.None;

        var currentType = SelectWallpaperThemeMode == ApplicationTheme.Light ? _builder.Config.WallpaperSwitch.Component.TypeLight : _builder.Config.WallpaperSwitch.Component.TypeDark;

        CurrentDisplayMode = currentType switch
        {
            WallpaperType.Global => WallpaperDisplayMode.Picture,
            WallpaperType.Individual => WallpaperDisplayMode.PictureMM,
            WallpaperType.SolidColor => WallpaperDisplayMode.SolidColor,
            WallpaperType.Spotlight => WallpaperDisplayMode.Spotlight,
            _ => WallpaperDisplayMode.Picture,
        };

        CurrentDisplayFlags = currentType switch
        {
            WallpaperType.Global => WallpaperDisplayFlags.PictureMode,
            WallpaperType.Individual => WallpaperDisplayFlags.PictureMMMode,
            WallpaperType.SolidColor => WallpaperDisplayFlags.SolidColorMode,
            WallpaperType.Spotlight => WallpaperDisplayFlags.SpotlightMode,
            _ => WallpaperDisplayFlags.None,
        };

        switch (currentType)
        {
            case WallpaperType.Global:
                GlobalWallpaperPath =
                            SelectWallpaperThemeMode == ApplicationTheme.Light ? _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light : _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark;
                GlobalWallpaperSource = GlobalWallpaperPath != null ? new BitmapImage(new Uri(GlobalWallpaperPath)) : (ImageSource?)null;
                break;
            case WallpaperType.Individual when SelectMonitor != null:
                {
                    var monitorSettings = (MonitorSettings)SelectMonitor;
                    GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light ? monitorSettings.LightThemeWallpaper : monitorSettings.DarkThemeWallpaper;
                    GlobalWallpaperSource = GlobalWallpaperPath != null ? new BitmapImage(new Uri(GlobalWallpaperPath)) : (ImageSource?)null;
                    break;
                }
            case WallpaperType.SolidColor:
                GlobalWallpaperSource = null;
                break;
            case WallpaperType.Spotlight:
                //TODO: Need a better method
                //GlobalWallpaperSource = new BitmapImage(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Themes\TranscodedWallpaper"));
                GlobalWallpaperSource = null;
                break;
        }

        ColorPreviewBorderBackground =
            SelectWallpaperThemeMode == ApplicationTheme.Light
                ? new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor())
                : new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor());
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        StateUpdateHandler.StopConfigWatcher();
        _dispatcherQueue.TryEnqueue(() =>
        {
            _builder.Load();
            LoadSettings();
        });
        StateUpdateHandler.StartConfigWatcher();
    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "PageApps");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
    }

    partial void OnIsWallpaperSwitchEnabledChanged(bool value)
    {
        _builder.Config.WallpaperSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    partial void OnSelectWallpaperThemeModeChanged(ApplicationTheme value)
    {
        LoadSettings();
    }

    partial void OnCurrentDisplayModeChanged(WallpaperDisplayMode value)
    {
        switch (SelectWallpaperThemeMode)
        {
            case ApplicationTheme.Light:
                _builder.Config.WallpaperSwitch.Component.TypeLight = CurrentDisplayMode switch
                {
                    WallpaperDisplayMode.Picture => WallpaperType.Global,
                    WallpaperDisplayMode.PictureMM => WallpaperType.Individual,
                    WallpaperDisplayMode.SolidColor => WallpaperType.SolidColor,
                    WallpaperDisplayMode.Spotlight => WallpaperType.Spotlight,
                    _ => WallpaperType.Unknown,
                };
                break;
            default:
                _builder.Config.WallpaperSwitch.Component.TypeDark = CurrentDisplayMode switch
                {
                    WallpaperDisplayMode.Picture => WallpaperType.Global,
                    WallpaperDisplayMode.PictureMM => WallpaperType.Individual,
                    WallpaperDisplayMode.SolidColor => WallpaperType.SolidColor,
                    WallpaperDisplayMode.Spotlight => WallpaperType.Spotlight,
                    _ => WallpaperType.Unknown,
                };
                break;
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
        RequestThemeSwitch();
    }

    partial void OnGlobalWallpaperPathChanged(string? value)
    {
        switch (CurrentDisplayMode)
        {
            case WallpaperDisplayMode.Picture:
                if (SelectWallpaperThemeMode == ApplicationTheme.Light)
                {
                    _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light = value;
                    _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Global;
                }
                else
                {
                    _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark = value;
                    _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Global;
                }
                break;
            case WallpaperDisplayMode.PictureMM when SelectMonitor != null:
                {
                    var monitorSettings = (MonitorSettings)SelectMonitor;
                    switch (SelectWallpaperThemeMode)
                    {
                        case ApplicationTheme.Light:
                            monitorSettings.LightThemeWallpaper = value;
                            _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Individual;
                            break;
                        default:
                            monitorSettings.DarkThemeWallpaper = value;
                            _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Individual;
                            break;
                    }

                    break;
                }
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    partial void OnSelectMonitorChanged(object? value)
    {
        if (CurrentDisplayMode == WallpaperDisplayMode.PictureMM && value != null)
        {
            MonitorSettings monitorSettings = (MonitorSettings)value;
            switch (SelectWallpaperThemeMode)
            {
                case ApplicationTheme.Light:
                    GlobalWallpaperPath = monitorSettings.LightThemeWallpaper;
                    break;
                default:
                    GlobalWallpaperPath = monitorSettings.DarkThemeWallpaper;
                    break;
            }
        }
    }

    partial void OnSelectWallpaperFillingModeChanged(WallpaperFillingMode value)
    {
        _builder.Config.WallpaperSwitch.Component.Position = value switch
        {
            WallpaperFillingMode.Fill => WallpaperPosition.Fill,
            WallpaperFillingMode.Fit => WallpaperPosition.Fit,
            WallpaperFillingMode.Stretch => WallpaperPosition.Stretch,
            WallpaperFillingMode.Center => WallpaperPosition.Center,
            _ => WallpaperPosition.Fill,
        };

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
