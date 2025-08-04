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

namespace AutoDarkModeApp.ViewModels;

public partial class WallpaperPickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;

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
    public partial bool SpotlightEnabled { get; set; }

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

        SelectWallpaperThemeMode = App.Current.RequestedTheme;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerViewModel");
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
            }
        });
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        bool hasUbr = int.TryParse(RegistryHandler.GetUbr(), out int ubr);
        if (
            hasUbr
            && (
                (Environment.OSVersion.Version.Build == (int)WindowsBuilds.Win11_22H2 && ubr >= (int)WindowsBuildsUbr.Win11_22H2_Spotlight)
                || Environment.OSVersion.Version.Build > (int)WindowsBuilds.Win11_22H2
            )
        )
            SpotlightEnabled = true;

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

        if (currentType == WallpaperType.Global)
        {
            GlobalWallpaperPath =
                SelectWallpaperThemeMode == ApplicationTheme.Light ? _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Light : _builder.Config.WallpaperSwitch.Component.GlobalWallpaper.Dark;
            if (GlobalWallpaperPath != null)
            {
                GlobalWallpaperSource = new BitmapImage(new Uri(GlobalWallpaperPath));
            }
            else
            {
                GlobalWallpaperSource = null;
            }
        }
        else if (currentType == WallpaperType.Individual && SelectMonitor != null)
        {
            MonitorSettings monitorSettings = (MonitorSettings)SelectMonitor;
            GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light ? monitorSettings.LightThemeWallpaper : monitorSettings.DarkThemeWallpaper;
            if (GlobalWallpaperPath != null)
            {
                GlobalWallpaperSource = new BitmapImage(new Uri(GlobalWallpaperPath));
            }
            else
            {
                GlobalWallpaperSource = null;
            }
        }
        else if (currentType == WallpaperType.SolidColor)
        {
            GlobalWallpaperSource = null;
        }
        else if (currentType == WallpaperType.Spotlight)
        {
            //TODO: It is necessary to determine the correct position of focusing wallpaper
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_24H2)
            {
                GlobalWallpaperPath = @"C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\DesktopSpotlight\Assets\Images\image_1.jpg";
                GlobalWallpaperSource = new BitmapImage(new Uri(GlobalWallpaperPath));
            }
            else if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_23H2)
            {
                GlobalWallpaperPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Themes\TranscodedWallpaper";
                GlobalWallpaperSource = new BitmapImage(new Uri(GlobalWallpaperPath));
            }
            else
            {
                GlobalWallpaperSource = null;
            }
        }

        ColorPreviewBorderBackground =
            SelectWallpaperThemeMode == ApplicationTheme.Light
                ? new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor())
                : new SolidColorBrush(_builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor());

        _isInitializing = false;
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
                throw new SwitchThemeException(result, "WallpaperPickerViewModel");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerViewModel");
        }
    }

    partial void OnIsWallpaperSwitchEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.WallpaperSwitch.Enabled = value;

        SafeSaveBuilder();
    }

    partial void OnSelectWallpaperThemeModeChanged(ApplicationTheme value)
    {
        if (_isInitializing)
            return;

        LoadSettings();
    }

    partial void OnCurrentDisplayModeChanged(WallpaperDisplayMode value)
    {
        if (_isInitializing)
            return;

        if (SelectWallpaperThemeMode == ApplicationTheme.Light)
        {
            _builder.Config.WallpaperSwitch.Component.TypeLight = CurrentDisplayMode switch
            {
                WallpaperDisplayMode.Picture => WallpaperType.Global,
                WallpaperDisplayMode.PictureMM => WallpaperType.Individual,
                WallpaperDisplayMode.SolidColor => WallpaperType.SolidColor,
                WallpaperDisplayMode.Spotlight => WallpaperType.Spotlight,
                _ => WallpaperType.Unknown,
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
                _ => WallpaperType.Unknown,
            };
        }

        SafeSaveBuilder();
        RequestThemeSwitch();

        // Here, respond and save the configuration file before refreshing the UI, in order to read the Spotlight wallpaper correctly
        LoadSettings();
    }

    partial void OnGlobalWallpaperPathChanged(string? value)
    {
        if (_isInitializing)
            return;

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

        SafeSaveBuilder();
    }

    partial void OnSelectMonitorChanged(object? value)
    {
        if (_isInitializing)
            return;

        if (CurrentDisplayMode == WallpaperDisplayMode.PictureMM && value != null)
        {
            MonitorSettings monitorSettings = (MonitorSettings)value;
            GlobalWallpaperPath = SelectWallpaperThemeMode == ApplicationTheme.Light ? monitorSettings.LightThemeWallpaper : monitorSettings.DarkThemeWallpaper;
            GlobalWallpaperSource = GlobalWallpaperPath != null ? new BitmapImage(new Uri(GlobalWallpaperPath)) : null;
        }
    }

    partial void OnSelectWallpaperFillingModeChanged(WallpaperFillingMode value)
    {
        if (_isInitializing)
            return;

        _builder.Config.WallpaperSwitch.Component.Position = value switch
        {
            WallpaperFillingMode.Fill => WallpaperPosition.Fill,
            WallpaperFillingMode.Fit => WallpaperPosition.Fit,
            WallpaperFillingMode.Stretch => WallpaperPosition.Stretch,
            WallpaperFillingMode.Center => WallpaperPosition.Center,
            _ => WallpaperPosition.Fill,
        };

        SafeSaveBuilder();
    }

    private void SafeSaveBuilder()
    {
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerViewModel");
        }
    }
}
