using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.ViewModels;

public partial class WallpaperPickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    public partial bool IsWallpaperSwitchEnabled { get; set; }

    [ObservableProperty]
    public partial int SelectIndexWallpaperMode { get; set; }

    [ObservableProperty]
    public partial bool IsPictureType { get; set; }

    [ObservableProperty]
    public partial bool IsPictureMMType { get; set; }

    [ObservableProperty]
    public partial bool IsSolidColorType { get; set; }

    [ObservableProperty]
    public partial bool IsSpotlightType { get; set; }

    [ObservableProperty]
    public partial Visibility ImageSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility MonitorSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility WallpaperFillingWaySettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility ColorSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial string? GlobalWallpaperPath { get; set; }

    [ObservableProperty]
    public partial object? SelectMonitor { get; set; }

    [ObservableProperty]
    public partial List<MonitorSettings>? Monitors { get; set; }

    [ObservableProperty]
    public partial int SelectIndexWallpaperFillingWay { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? SetColorButtonForeground { get; set; }

    public ICommand PickFileCommand { get; }

    public WallpaperPickerViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
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
        SelectIndexWallpaperFillingWay = _builder.Config.WallpaperSwitch.Component.Position switch
        {
            WallpaperPosition.Fill => 0,
            WallpaperPosition.Fit => 1,
            WallpaperPosition.Stretch => 2,
            _ => 0
        };

        ResetAllOptions();

        // Generate a list with all installed Monitors, select the first one
        List<MonitorSettings> monitors = _builder.Config.WallpaperSwitch.Component.Monitors;
        List<MonitorSettings> disconnected = new();
        List<MonitorSettings> connected = monitors.Where(m =>
        {
            // Preload tostring to avoid dropdown opening lag
            m.ToString();
            // Return monitors connecte to system connected monitors
            if (m.Connected)
            {
                return true;
            }
            disconnected.Add(m);
            return false;
        }).ToList();

        disconnected.ForEach(m =>
        {
            m.MonitorString = $"{"DisplayMonitorDisconnected".GetLocalized()} - {m.MonitorString}";
        });

        monitors.Clear();
        monitors.AddRange(connected);
        monitors.AddRange(disconnected);
        Monitors = monitors;

        if (Monitors.Count > 0)
        {
            SelectMonitor = Monitors[0];
        }

        if (SelectIndexWallpaperMode == 0)
        {
            switch (_builder.Config.WallpaperSwitch.Component.TypeLight)
            {
                case WallpaperType.Global:
                    IsPictureType = true;
                    ImageSettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.Individual:
                    IsPictureMMType = true;
                    ImageSettingsCardVisibility = Visibility.Visible;
                    MonitorSettingsCardVisibility = Visibility.Visible;
                    WallpaperFillingWaySettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.SolidColor:
                    IsSolidColorType = true;
                    ColorSettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.Spotlight:
                    IsSpotlightType = true;
                    break;
            }

            if (IsPictureType == true)
            {
                GlobalWallpaperPath = _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light;
            }
            else if (IsPictureMMType == true)
            {
                MonitorSettings monitorSettings = (SelectMonitor != null) ? (MonitorSettings)SelectMonitor : (MonitorSettings)new object();
                GlobalWallpaperPath = monitorSettings.LightThemeWallpaper;
            }

            SetColorButtonForeground = new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor());
        }
        else
        {
            switch (_builder.Config.WallpaperSwitch.Component.TypeDark)
            {
                case WallpaperType.Global:
                    IsPictureType = true;
                    ImageSettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.Individual:
                    IsPictureMMType = true;
                    ImageSettingsCardVisibility = Visibility.Visible;
                    MonitorSettingsCardVisibility = Visibility.Visible;
                    WallpaperFillingWaySettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.SolidColor:
                    IsSolidColorType = true;
                    ColorSettingsCardVisibility = Visibility.Visible;
                    break;
                case WallpaperType.Spotlight:
                    IsSpotlightType = true;
                    break;
            }

            if (IsPictureType == true)
            {
                GlobalWallpaperPath = _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark;
            }
            else if (IsPictureMMType == true)
            {
                MonitorSettings monitorSettings = (SelectMonitor != null) ? (MonitorSettings)SelectMonitor : (MonitorSettings)new object();
                GlobalWallpaperPath = monitorSettings.DarkThemeWallpaper;
            }

            SetColorButtonForeground = new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor());
        }
    }

    private void ResetAllOptions()
    {
        IsPictureType = false;
        IsPictureMMType = false;
        IsSolidColorType = false;
        IsSpotlightType = false;
        ImageSettingsCardVisibility = Visibility.Collapsed;
        MonitorSettingsCardVisibility = Visibility.Collapsed;
        WallpaperFillingWaySettingsCardVisibility = Visibility.Collapsed;
        ColorSettingsCardVisibility = Visibility.Collapsed;
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnSelectIndexWallpaperModeChanged(int value)
    {
        LoadSettings();
    }

    partial void OnIsPictureTypeChanged(bool value)
    {
        if (SelectIndexWallpaperMode == 0 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Global;
        }
        else if (SelectIndexWallpaperMode == 1 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Global;
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnIsPictureMMTypeChanged(bool value)
    {
        if (SelectIndexWallpaperMode == 0 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Individual;
        }
        else if (SelectIndexWallpaperMode == 1 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Individual;
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnIsSolidColorTypeChanged(bool value)
    {
        if (SelectIndexWallpaperMode == 0 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.SolidColor;
        }
        else if (SelectIndexWallpaperMode == 1 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.SolidColor;
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnIsSpotlightTypeChanged(bool value)
    {
        if (SelectIndexWallpaperMode == 0 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = WallpaperType.Spotlight;
        }
        else if (SelectIndexWallpaperMode == 1 && value == true)
        {
            _builder.Config.WallpaperSwitch.Component.TypeDark = WallpaperType.Spotlight;
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnGlobalWallpaperPathChanged(string? value)
    {
        if (IsPictureType == true)
        {
            if (SelectIndexWallpaperMode == 0)
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
        else if (IsPictureMMType == true)
        {
            MonitorSettings monitorSettings = (SelectMonitor != null) ? (MonitorSettings)SelectMonitor : (MonitorSettings)new object();
            if (SelectIndexWallpaperMode == 0)
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }

    partial void OnSelectIndexWallpaperFillingWayChanged(int value)
    {
        _builder.Config.WallpaperSwitch.Component.Position = value switch
        {
            0 => WallpaperPosition.Fill,
            1 => WallpaperPosition.Fit,
            2 => WallpaperPosition.Stretch,
            _ => WallpaperPosition.Fill
        };
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }
}
