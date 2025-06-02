using System.Collections.ObjectModel;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.System.Power;

namespace AutoDarkModeApp.ViewModels;

public partial class ConditionsViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private readonly DispatcherQueueTimer _debounceTimer;
    private bool _isInitializing;

    [ObservableProperty]
    public partial bool GPUMonitoringEnabled { get; set; }

    [ObservableProperty]
    public partial int GPUMonitoringThreshold { get; set; }

    [ObservableProperty]
    public partial int GPUMonitoringSamples { get; set; }

    [ObservableProperty]
    public partial bool ProcessBlockEnabled { get; set; }

    [ObservableProperty]
    public partial List<string>? ProcessListItemSource { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string>? ProcessBlockListItemSource { get; set; }

    [ObservableProperty]
    public partial bool IdleCheckerEnabled { get; set; }

    [ObservableProperty]
    public partial int IdleCheckerThreshold { get; set; }

    [ObservableProperty]
    public partial bool AutoSwitchNotifyEnabled { get; set; }

    [ObservableProperty]
    public partial int AutoSwitchNotifyGracePeriodMinutes { get; set; }

    [ObservableProperty]
    public partial bool BatterySettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial bool BatteryDarkModeEnabled { get; set; }

    [ObservableProperty]
    public partial bool HotkeysEnabled { get; set; }

    [ObservableProperty]
    public partial bool SettingsCardEnabled { get; set; }

    [ObservableProperty]
    public partial string? HotkeyForceLight { get; set; }

    [ObservableProperty]
    public partial string? HotkeyForceDark { get; set; }

    [ObservableProperty]
    public partial string? HotkeyNoForce { get; set; }

    [ObservableProperty]
    public partial string? HotkeyToggleTheme { get; set; }

    [ObservableProperty]
    public partial string? HotkeyToggleAutomaticTheme { get; set; }

    [ObservableProperty]
    public partial bool HotkeyToggleAutomaticThemeNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial string? HotkeyTogglePostpone { get; set; }

    [ObservableProperty]
    public partial bool HotkeyTogglePostponeNotificationEnabled { get; set; }

    public ConditionsViewModel(IErrorService errorService)
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }

        LoadSettings();

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
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
            }
            _debounceTimer.Stop();
        };

        StateUpdateHandler.AddDebounceEventOnConfigUpdate(() => HandleConfigUpdate());
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        GPUMonitoringEnabled = _builder.Config.GPUMonitoring.Enabled;
        GPUMonitoringThreshold = _builder.Config.GPUMonitoring.Threshold;
        GPUMonitoringSamples = _builder.Config.GPUMonitoring.Samples - 1;
        ProcessBlockEnabled = _builder.Config.ProcessBlockList.Enabled;
        ProcessBlockListItemSource ??= new ObservableCollection<string>(_builder.Config.ProcessBlockList.ProcessNames);
        IdleCheckerEnabled = _builder.Config.IdleChecker.Enabled;
        IdleCheckerThreshold = _builder.Config.IdleChecker.Threshold;
        AutoSwitchNotifyEnabled = _builder.Config.AutoSwitchNotify.Enabled;
        AutoSwitchNotifyGracePeriodMinutes = _builder.Config.AutoSwitchNotify.GracePeriodMinutes;
        HotkeysEnabled = _builder.Config.Hotkeys.Enabled;
        SettingsCardEnabled = !HotkeysEnabled; // TODO: Give this a better name
        BatterySettingsCardVisibility = PowerManager.BatteryStatus != BatteryStatus.NotPresent;
        BatteryDarkModeEnabled = _builder.Config.Events.DarkThemeOnBattery;
        HotkeyForceLight = _builder.Config.Hotkeys.ForceLight;
        HotkeyForceDark = _builder.Config.Hotkeys.ForceDark;
        HotkeyNoForce = _builder.Config.Hotkeys.NoForce;
        HotkeyToggleTheme = _builder.Config.Hotkeys.ToggleTheme;
        HotkeyToggleAutomaticTheme = _builder.Config.Hotkeys.ToggleAutoThemeSwitch;
        HotkeyToggleAutomaticThemeNotificationEnabled = _builder.Config.Notifications.OnAutoThemeSwitching;
        HotkeyTogglePostpone = _builder.Config.Hotkeys.TogglePostpone;
        HotkeyTogglePostponeNotificationEnabled = _builder.Config.Notifications.OnSkipNextSwitch;

        _isInitializing = false;
    }

    private void RequestDebouncedSave()
    {
        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    private void HandleConfigUpdate()
    {
        StateUpdateHandler.StopConfigWatcher();
        _dispatcherQueue.TryEnqueue(() =>
        {
            _builder.Load();
            LoadSettings();
        });
        StateUpdateHandler.StartConfigWatcher();
    }

    partial void OnGPUMonitoringEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.GPUMonitoring.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnGPUMonitoringThresholdChanged(int value)
    {
        if (_isInitializing)
            return;

        _builder.Config.GPUMonitoring.Threshold = value;
        RequestDebouncedSave();
    }

    partial void OnGPUMonitoringSamplesChanged(int value)
    {
        if (_isInitializing)
            return;

        _builder.Config.GPUMonitoring.Samples = value + 1;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnProcessBlockEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.ProcessBlockList.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnIdleCheckerEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.IdleChecker.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnIdleCheckerThresholdChanged(int value)
    {
        if (_isInitializing)
            return;

        _builder.Config.IdleChecker.Threshold = value;
        RequestDebouncedSave();
    }

    partial void OnAutoSwitchNotifyEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.AutoSwitchNotify.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnAutoSwitchNotifyGracePeriodMinutesChanged(int value)
    {
        if (_isInitializing)
            return;

        _builder.Config.AutoSwitchNotify.GracePeriodMinutes = value;
        RequestDebouncedSave();
    }

    partial void OnBatteryDarkModeEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Events.DarkThemeOnBattery = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnHotkeysEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Hotkeys.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnHotkeyToggleAutomaticThemeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Notifications.OnAutoThemeSwitching = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }

    partial void OnHotkeyTogglePostponeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Notifications.OnSkipNextSwitch = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SwitchModesViewModel");
        }
    }
}
