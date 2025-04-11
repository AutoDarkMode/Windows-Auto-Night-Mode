using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class AppsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private int _appsSwitchComponentMode;

    [ObservableProperty]
    private int _systemSwitchComponentMode;

    [ObservableProperty]
    private bool _isAdaptiveTaskbarAccent;

    [ObservableProperty]
    private bool _isTaskbarColorOnAdaptive;

    [ObservableProperty]
    private bool _isAdaptiveTaskbarAccentOnLight;

    [ObservableProperty]
    private bool _isAdaptiveTaskbarAccentOnDark;

    [ObservableProperty]
    private bool _isDWMPrevalenceSwitch;

    [ObservableProperty]
    private bool _isDWMPrevalenceEnableThemeLight;

    [ObservableProperty]
    private bool _isDWMPrevalenceEnableThemeDark;

    [ObservableProperty]
    private bool _isTouchKeyboardSwitch;

    [ObservableProperty]
    private bool _isColorFilterSwitch;

    public AppsViewModel(IErrorService errorService)
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        if (_builder.Config.AppsSwitch.Enabled)
        {
            AppsSwitchComponentMode = _builder.Config.AppsSwitch.Component.Mode switch
            {
                Mode.Switch => 0,
                Mode.LightOnly => 1,
                Mode.DarkOnly => 2,
                _ => 0,
            };
        }
        else
        {
            AppsSwitchComponentMode = 3;
        }

        if (_builder.Config.SystemSwitch.Enabled)
        {
            SystemSwitchComponentMode = _builder.Config.SystemSwitch.Component.Mode switch
            {
                Mode.Switch => 0,
                Mode.LightOnly => 1,
                Mode.DarkOnly => 2,
                Mode.AccentOnly => 3,
                _ => 0,
            };
            if (SystemSwitchComponentMode == 3)
            {
                IsAdaptiveTaskbarAccent = true;
            }
            else
            {
                IsAdaptiveTaskbarAccent = false;
            }
        }
        else
        {
            SystemSwitchComponentMode = 4;
        }

        IsTaskbarColorOnAdaptive = _builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive;
        if (_builder.Config.SystemSwitch.Component.TaskbarColorWhenNonAdaptive == Theme.Light)
        {
            IsAdaptiveTaskbarAccentOnLight = true;
            IsAdaptiveTaskbarAccentOnDark = false;
        }
        else
        {
            IsAdaptiveTaskbarAccentOnLight = false;
            IsAdaptiveTaskbarAccentOnDark = true;
        }

        IsDWMPrevalenceSwitch = _builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch;
        if (_builder.Config.SystemSwitch.Component.DWMPrevalenceEnableTheme == Theme.Light)
        {
            IsDWMPrevalenceEnableThemeLight = true;
            IsDWMPrevalenceEnableThemeDark = false;
        }
        else
        {
            IsDWMPrevalenceEnableThemeLight = false;
            IsDWMPrevalenceEnableThemeDark = true;
        }

        IsTouchKeyboardSwitch = _builder.Config.TouchKeyboardSwitch.Enabled;
        IsColorFilterSwitch = _builder.Config.ColorFilterSwitch.Enabled;
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

    partial void OnAppsSwitchComponentModeChanged(int value)
    {
        if (value != 3)
        {
            _builder.Config.AppsSwitch.Enabled = true;
            _builder.Config.AppsSwitch.Component.Mode = value switch
            {
                0 => Mode.Switch,
                1 => Mode.LightOnly,
                2 => Mode.DarkOnly,
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
    }

    partial void OnSystemSwitchComponentModeChanged(int value)
    {
        if (value != 4)
        {
            _builder.Config.SystemSwitch.Enabled = true;
            _builder.Config.SystemSwitch.Component.Mode = value switch
            {
                0 => Mode.Switch,
                1 => Mode.LightOnly,
                2 => Mode.DarkOnly,
                3 => Mode.AccentOnly,
                _ => Mode.Switch,
            };
            if (value == 3)
            {
                IsAdaptiveTaskbarAccent = true;
            }
            else
            {
                IsAdaptiveTaskbarAccent = false;
            }
        }
        else
        {
            _builder.Config.SystemSwitch.Enabled = false;
        }
    }

    partial void OnIsTaskbarColorOnAdaptiveChanged(bool value)
    {
        _builder.Config.SystemSwitch.Component.TaskbarColorOnAdaptive = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
    }

    partial void OnIsAdaptiveTaskbarAccentOnLightChanged(bool value)
    {
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnIsAdaptiveTaskbarAccentOnDarkChanged(bool value)
    {
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceSwitchChanged(bool value)
    {
        _builder.Config.SystemSwitch.Component.DWMPrevalenceSwitch = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceEnableThemeLightChanged(bool value)
    {
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnIsDWMPrevalenceEnableThemeDarkChanged(bool value)
    {
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
        RequestThemeSwitch();
    }

    partial void OnIsTouchKeyboardSwitchChanged(bool value)
    {
        _builder.Config.TouchKeyboardSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
    }

    partial void OnIsColorFilterSwitchChanged(bool value)
    {
        _builder.Config.ColorFilterSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AppsPage");
        }
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
