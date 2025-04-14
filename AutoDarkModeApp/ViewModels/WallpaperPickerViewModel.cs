using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
        Spotlight
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
        PictureMode = ShowImageSettings,
        PictureMMMode = ShowImageSettings | ShowMonitorSettings | ShowFillingWaySettings,
        SolidColorMode = ShowColorSettings,
        SpotlightMode = ShowSpotlight
    }

    public enum WallpaperFillingMode
    {
        Fill,
        Fit,
        Stretch
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

        SelectWallpaperThemeMode = App.Current.RequestedTheme;

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
                FileTypeFilter = { ".jpg", ".jpeg", ".bmp", ".dib", ".png", ".jff", ".jpe", ".gif", ".tif", ".tiff", ".wdp", ".heic", ".heif", ".heics", ".heifs", ".hif", ".avci", ".avcs", ".avif", ".avifs", ".jxr", ".jxl" }
            };
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                GlobalWallpaperPath = file.Path;
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
            _ => WallpaperFillingMode.Fill
        };

        CurrentDisplayFlags = WallpaperDisplayFlags.None;

        var currentType = SelectWallpaperThemeMode == ApplicationTheme.Light
            ? _builder.Config.WallpaperSwitch.Component.TypeLight
            : _builder.Config.WallpaperSwitch.Component.TypeDark;

        CurrentDisplayMode = currentType switch
        {
            WallpaperType.Global => WallpaperDisplayMode.Picture,
            WallpaperType.Individual => WallpaperDisplayMode.PictureMM,
            WallpaperType.SolidColor => WallpaperDisplayMode.SolidColor,
            WallpaperType.Spotlight => WallpaperDisplayMode.Spotlight,
            _ => WallpaperDisplayMode.Picture
        };

        CurrentDisplayFlags = currentType switch
        {
            WallpaperType.Global => WallpaperDisplayFlags.PictureMode,
            WallpaperType.Individual => WallpaperDisplayFlags.PictureMMMode,
            WallpaperType.SolidColor => WallpaperDisplayFlags.SolidColorMode,
            WallpaperType.Spotlight => WallpaperDisplayFlags.SpotlightMode,
            _ => WallpaperDisplayFlags.None
        };

        if (currentType == WallpaperType.Global)
        {
            GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light
                ? _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light
                : _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark;
        }
        else if (currentType == WallpaperType.Individual && SelectMonitor != null)
        {
            MonitorSettings monitorSettings = (MonitorSettings)SelectMonitor;
            GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light
                ? monitorSettings.LightThemeWallpaper
                : monitorSettings.DarkThemeWallpaper;
        }
        else
        {
            GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light
                ? _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light
                : _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark;
        }

        ColorPreviewBorderBackground = SelectWallpaperThemeMode == ApplicationTheme.Light
            ? new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor())
            : new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor());
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StateUpdateHandler.StopConfigWatcher();

            _builder.Load();
            LoadSettings();

            StateUpdateHandler.StartConfigWatcher();
        });
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
        if (SelectWallpaperThemeMode == ApplicationTheme.Light)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = CurrentDisplayMode switch
            {
                WallpaperDisplayMode.Picture => WallpaperType.Global,
                WallpaperDisplayMode.PictureMM => WallpaperType.Individual,
                WallpaperDisplayMode.SolidColor => WallpaperType.SolidColor,
                WallpaperDisplayMode.Spotlight => WallpaperType.Spotlight,
                _ => WallpaperType.Unknown
            };
        }
        else
        {
            _builder.Config.WallpaperSwitch.Component.TypeDark = CurrentDisplayMode switch
            {
                WallpaperDisplayMode.Picture => WallpaperType.Global,
                WallpaperDisplayMode.PictureMM => WallpaperType.Individual,
                WallpaperDisplayMode.SolidColor => WallpaperType.SolidColor,
                WallpaperDisplayMode.Spotlight => WallpaperType.Spotlight,
                _ => WallpaperType.Unknown
            };
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

    partial void OnGlobalWallpaperPathChanged(string? value)
    {
        if (CurrentDisplayMode == WallpaperDisplayMode.Picture)
        {
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
        }
        else if (CurrentDisplayMode == WallpaperDisplayMode.PictureMM && SelectMonitor != null)
        {
            MonitorSettings monitorSettings = (MonitorSettings)SelectMonitor;
            if (SelectWallpaperThemeMode == ApplicationTheme.Light)
            {
                monitorSettings.LightThemeWallpaper = value;
                _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Individual;
            }
            else
            {
                monitorSettings.DarkThemeWallpaper = value;
                _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Individual;
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
        if (CurrentDisplayMode == WallpaperDisplayMode.PictureMM)
        {
            if (CurrentDisplayMode == WallpaperDisplayMode.PictureMM && value != null)
            {
                MonitorSettings monitorSettings = (MonitorSettings)value;
                if (SelectWallpaperThemeMode == ApplicationTheme.Light)
                {
                    GlobalWallpaperPath = monitorSettings.LightThemeWallpaper;
                }
                else
                {
                    GlobalWallpaperPath = monitorSettings.DarkThemeWallpaper;
                }
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
            _ => WallpaperPosition.Fill
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
