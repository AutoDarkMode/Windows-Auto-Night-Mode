using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class SystemAreasViewModel : ObservableRecipient
{
    private const string Source = "SystemAreasViewModel";
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;

    public enum AppSwitchMode
    {
        AdaptToSystem,
        AlwaysLight,
        AlwaysDark,
        Disabled,
    }

    public enum SystemSwitchMode
    {
        AdaptToSystem,
        AlwaysLight,
        AlwaysDark,
        AccentOnly,
        Disabled,
    }

    [ObservableProperty]
    public partial AppSwitchMode AppsSwitchComponentMode { get; set; }

    [ObservableProperty]
    public partial SystemSwitchMode SystemSwitchComponentMode { get; set; }

    [ObservableProperty]
    public partial bool AccentColorForTaskbarSettingsCardVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTaskbarColorOnAdaptive { get; set; }

    [ObservableProperty]
    public partial bool IsAdaptiveTaskbarAccent { get; set; }

    [ObservableProperty]
    public partial bool IsAdaptiveTaskbarAccentOnLight { get; set; }

    [ObservableProperty]
    public partial bool IsAdaptiveTaskbarAccentOnDark { get; set; }

    [ObservableProperty]
    public partial bool IsDWMPrevalenceSwitch { get; set; }

    [ObservableProperty]
    public partial bool IsDWMPrevalenceEnableThemeLight { get; set; }

    [ObservableProperty]
    public partial bool IsDWMPrevalenceEnableThemeDark { get; set; }

    [ObservableProperty]
    public partial bool IsTouchKeyboardSwitch { get; set; }

    [ObservableProperty]
    public partial bool IsColorFilterSwitch { get; set; }

    public SystemAreasViewModel(IErrorService errorService)
    {
        _errorService = errorService;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        if (_builder.Config.AppsSwitch.Enabled)
        {
            AppsSwitchComponentMode = _builder.Config.AppsSwitch.Component.Mode switch
            {
                Mode.Switch => AppSwitchMode.AdaptToSystem,
                Mode.LightOnly => AppSwitchMode.AlwaysLight,
                Mode.DarkOnly => AppSwitchMode.AlwaysDark,
                _ => AppSwitchMode.Disabled,
            };
        }
        else
        {
            AppsSwitchComponentMode = AppSwitchMode.Disabled;
        }

        if (_builder.Config.SystemSwitch.Enabled)
        {
            SystemSwitchComponentMode = _builder.Config.SystemSwitch.Component.Mode switch
            {
                Mode.Switch => SystemSwitchMode.AdaptToSystem,
                Mode.LightOnly => SystemSwitchMode.AlwaysLight,
                Mode.DarkOnly => SystemSwitchMode.AlwaysDark,
                Mode.AccentOnly => SystemSwitchMode.AccentOnly,
                _ => SystemSwitchMode.Disabled,
            };
        }
        else
        {
            SystemSwitchComponentMode = SystemSwitchMode.Disabled;
        }
        AccentColorForTaskbarSettingsCardVisible =
            SystemSwitchComponentMode != SystemSwitchMode.AlwaysLight && SystemSwitchComponentMode != SystemSwitchMode.AccentOnly && SystemSwitchComponentMode != SystemSwitchMode.Disabled;
        IsAdaptiveTaskbarAccent = SystemSwitchComponentMode == SystemSwitchMode.AccentOnly;

        IsTaskbarColorOnAdaptive = _builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive;
        switch (_builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive)
        {
            case Theme.Light:
                IsAdaptiveTaskbarAccentOnLight = true;
                IsAdaptiveTaskbarAccentOnDark = false;
                break;
            default:
                IsAdaptiveTaskbarAccentOnLight = false;
                IsAdaptiveTaskbarAccentOnDark = true;
                break;
        }

        IsDWMPrevalenceSwitch = _builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch;
        switch (_builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme)
        {
            case Theme.Light:
                IsDWMPrevalenceEnableThemeLight = true;
                IsDWMPrevalenceEnableThemeDark = false;
                break;
            default:
                IsDWMPrevalenceEnableThemeLight = false;
                IsDWMPrevalenceEnableThemeDark = true;
                break;
        }

        IsTouchKeyboardSwitch = _builder.Config.TouchKeyboardSwitch.Enabled;
        IsColorFilterSwitch = _builder.Config.ColorFilterSwitch.Enabled;

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

    // TODO: Different processing methods from TimeViewModel
    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, Source);
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }
    }

    partial void OnAppsSwitchComponentModeChanged(AppSwitchMode value)
    {
        if (_isInitializing)
            return;

        if (value != AppSwitchMode.Disabled)
        {
            _builder.Config.AppsSwitch.Enabled = true;
            _builder.Config.AppsSwitch.Component.Mode = value switch
            {
                AppSwitchMode.AdaptToSystem => Mode.Switch,
                AppSwitchMode.AlwaysLight => Mode.LightOnly,
                AppSwitchMode.AlwaysDark => Mode.DarkOnly,
                _ => Mode.Switch,
            };
        }
        else
        {
            _builder.Config.AppsSwitch.Enabled = false;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnSystemSwitchComponentModeChanged(SystemSwitchMode value)
    {
        if (_isInitializing)
            return;

        if (value != SystemSwitchMode.Disabled)
        {
            _builder.Config.SystemSwitch.Enabled = true;
            _builder.Config.SystemSwitch.Component.Mode = value switch
            {
                SystemSwitchMode.AdaptToSystem => Mode.Switch,
                SystemSwitchMode.AlwaysLight => Mode.LightOnly,
                SystemSwitchMode.AlwaysDark => Mode.DarkOnly,
                SystemSwitchMode.AccentOnly => Mode.AccentOnly,
                _ => Mode.Switch,
            };
        }
        else
        {
            _builder.Config.SystemSwitch.Enabled = false;
        }
        AccentColorForTaskbarSettingsCardVisible =
            SystemSwitchComponentMode != SystemSwitchMode.AlwaysLight && SystemSwitchComponentMode != SystemSwitchMode.AccentOnly && SystemSwitchComponentMode != SystemSwitchMode.Disabled;
        IsAdaptiveTaskbarAccent =
        value == SystemSwitchMode.AccentOnly;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsTaskbarColorOnAdaptiveChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive = value;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsAdaptiveTaskbarAccentOnLightChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            _builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive = Theme.Light;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsAdaptiveTaskbarAccentOnDarkChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            _builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive = Theme.Dark;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceSwitchChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch = value;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceEnableThemeLightChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            _builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme = Theme.Light;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceEnableThemeDarkChanged(bool value)
    {
        if (_isInitializing)
            return;

        if (value)
        {
            _builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme = Theme.Dark;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }

        RequestThemeSwitch();
    }

    partial void OnIsTouchKeyboardSwitchChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.TouchKeyboardSwitch.Enabled = value;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }
    }

    partial void OnIsColorFilterSwitchChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.ColorFilterSwitch.Enabled = value;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Source);
        }
    }
}
