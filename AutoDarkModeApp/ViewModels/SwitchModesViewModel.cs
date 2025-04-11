using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Navigation;
using Windows.System.Power;

namespace AutoDarkModeApp.ViewModels;

public partial class SwitchModesViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    private DispatcherQueueTimer _debounceTimer; // Debounce

    [ObservableProperty]
    private bool _isGPUMonitoring;

    [ObservableProperty]
    private int _gPUMonitoringThreshold;

    [ObservableProperty]
    private int _gPUMonitoringSamples;

    [ObservableProperty]
    private bool _isIdleChecker;

    [ObservableProperty]
    private int _idleCheckerThreshold;

    [ObservableProperty]
    private bool _isAutoSwitchNotify;

    [ObservableProperty]
    private int _autoSwitchNotifyGracePeriodMinutes;

    [ObservableProperty]
    private bool _isBatteryDarkModeEnable;

    [ObservableProperty]
    private bool _isBatteryDarkMode;

    [ObservableProperty]
    private bool _isHotkeysEnabled;

    [ObservableProperty]
    private bool _isSettingsCardEnabled;

    [ObservableProperty]
    private string? _hotkeyForceLight;

    [ObservableProperty]
    private string? _hotkeyForceDark;

    [ObservableProperty]
    private string? _hotkeyNoForce;

    [ObservableProperty]
    private string? _hotkeyToggleTheme;

    [ObservableProperty]
    private string? _hotkeyToggleAutomaticTheme;

    [ObservableProperty]
    private bool _isHotkeyToggleAutomaticThemeNotification;

    [ObservableProperty]
    private string? _hotkeyTogglePostpone;

    [ObservableProperty]
    private bool _isHotkeyTogglePostponeNotification;

    // TODO: The logic part about BatteryDarkMode is not written
    public SwitchModesViewModel(IErrorService errorService)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

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

        // Delay 500ms
        _debounceTimer = _dispatcherQueue.CreateTimer();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(500);
        _debounceTimer.Tick += (s, e) =>
        {
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
            }
            _debounceTimer.Stop();
        };

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        IsGPUMonitoring = _builder.Config.GPUMonitoring.Enabled;
        GPUMonitoringThreshold = _builder.Config.GPUMonitoring.Threshold;
        GPUMonitoringSamples = _builder.Config.GPUMonitoring.Samples - 1;
        IsIdleChecker = _builder.Config.IdleChecker.Enabled;
        IdleCheckerThreshold = _builder.Config.IdleChecker.Threshold;
        IsAutoSwitchNotify = _builder.Config.AutoSwitchNotify.Enabled;
        AutoSwitchNotifyGracePeriodMinutes = _builder.Config.AutoSwitchNotify.GracePeriodMinutes;
        IsHotkeysEnabled = _builder.Config.Hotkeys.Enabled;
        IsSettingsCardEnabled = !IsHotkeysEnabled; // TODO: give this a better name
        IsBatteryDarkMode = _builder.Config.Events.DarkThemeOnBattery;
        HotkeyForceLight = _builder.Config.Hotkeys.ForceLight;
        HotkeyForceDark = _builder.Config.Hotkeys.ForceDark;
        HotkeyNoForce = _builder.Config.Hotkeys.NoForce;
        HotkeyToggleTheme = _builder.Config.Hotkeys.ToggleTheme;
        HotkeyToggleAutomaticTheme = _builder.Config.Hotkeys.ToggleAutoThemeSwitch;
        IsHotkeyToggleAutomaticThemeNotification = _builder.Config.Notifications.OnAutoThemeSwitching;
        HotkeyTogglePostpone = _builder.Config.Hotkeys.TogglePostpone;
        IsHotkeyTogglePostponeNotification = _builder.Config.Notifications.OnSkipNextSwitch;
        if (PowerManager.BatteryStatus == BatteryStatus.NotPresent)
        {
            IsBatteryDarkModeEnable = false;
        }
    }

    private void RequestDebouncedSave()
    {
        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
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

    partial void OnIsGPUMonitoringChanged(bool value)
    {
        _builder.Config.GPUMonitoring.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnGPUMonitoringThresholdChanged(int value)
    {
        _builder.Config.GPUMonitoring.Threshold = value;
        RequestDebouncedSave();
    }

    partial void OnGPUMonitoringSamplesChanged(int value)
    {
        _builder.Config.GPUMonitoring.Samples = value + 1;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnIsIdleCheckerChanged(bool value)
    {
        _builder.Config.IdleChecker.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnIdleCheckerThresholdChanged(int value)
    {
        _builder.Config.IdleChecker.Threshold = value;
        RequestDebouncedSave();
    }

    partial void OnIsAutoSwitchNotifyChanged(bool value)
    {
        _builder.Config.AutoSwitchNotify.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnAutoSwitchNotifyGracePeriodMinutesChanged(int value)
    {
        _builder.Config.AutoSwitchNotify.GracePeriodMinutes = value;
        RequestDebouncedSave();
    }

    partial void OnIsBatteryDarkModeChanged(bool value)
    {
        _builder.Config.Events.DarkThemeOnBattery = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnIsHotkeysEnabledChanged(bool value)
    {
        _builder.Config.Hotkeys.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnIsHotkeyToggleAutomaticThemeNotificationChanged(bool value)
    {
        _builder.Config.Notifications.OnAutoThemeSwitching = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    partial void OnIsHotkeyTogglePostponeNotificationChanged(bool value)
    {
        _builder.Config.Notifications.OnSkipNextSwitch = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModePage");
        }
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
