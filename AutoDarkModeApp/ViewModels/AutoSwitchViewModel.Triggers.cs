namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool AutoThemeSwitchingEnabled { get; set; }

    [ObservableProperty]
    public partial SwitchTriggerMode SelectedTriggerMode { get; set; }

    [ObservableProperty]
    public partial Visibility OffsetTimeSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial int OffsetTimesMinimum { get; set; }

    [ObservableProperty]
    public partial int OffsetLight { get; set; }

    [ObservableProperty]
    public partial int OffsetDark { get; set; }

    [RelayCommand]
    private void SetTriggerMode(string mode)
    {
        if (Enum.TryParse<SwitchTriggerMode>(mode, out var result))
        {
            SelectedTriggerMode = result;
        }
    }

    private void HandleAutoTheme(bool value)
    {
        AutoThemeSwitchingEnabled = value;
        var mode = _builder.Config.Governor switch
        {
            Governor.NightLight => SwitchTriggerMode.WindowsNightLight,
            Governor.AmbientLight => SwitchTriggerMode.AmbientLight,
            _ when _builder.Config.Location.Enabled && _builder.Config.Location.UseGeolocatorService => SwitchTriggerMode.LocationTimes,
            _ when _builder.Config.Location.Enabled => SwitchTriggerMode.CoordinateTimes,
            _ => SwitchTriggerMode.CustomTimes,
        };
        SelectedTriggerMode = mode;
        ApplyTriggerModeUiState(TriggerModeSettings.Get(mode));
    }

    private void ApplyTriggerModeUiState(TriggerModeSettings settings)
    {
        LocationSettingsCardVisibility = settings.LocationSettingsVisibility;
        CustomTimeSettingsCardVisibility = settings.CustomTimeSettingsVisibility;
        OffsetTimeSettingsCardVisibility = settings.OffsetTimeSettingsVisibility;
        PostponeOptionsSkipOnceVisibility = settings.PostponeOptionsSkipOnceVisibility;
        OffsetTimesMinimum = settings.OffsetMinimum;
    }

    partial void OnAutoThemeSwitchingEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        HandleAutoTheme(value);

        _builder.Config.AutoThemeSwitchingEnabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }
    }

    partial void OnSelectedTriggerModeChanged(SwitchTriggerMode value)
    {
        if (_isInitializing)
            return;

        var settings = TriggerModeSettings.Get(value);

        if (value == SwitchTriggerMode.AmbientLight
            && _builder.Config.AmbientLight.DarkThreshold == 40
            && _builder.Config.AmbientLight.LightThreshold == 80)
        {
            AutoConfigure();
        }

        _builder.Config.Governor = settings.Governor;
        _builder.Config.Location.Enabled = settings.LocationEnabled;
        _builder.Config.Location.UseGeolocatorService = settings.UseGeolocatorService;
        if (settings.ForcesAutoThemeSwitching)
        {
            _builder.Config.AutoThemeSwitchingEnabled = true;
        }

        ApplyTriggerModeUiState(settings);

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        RequestThemeSwitch();
    }

    partial void OnOffsetLightChanged(int value)
    {
        if (_isInitializing)
            return;

        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    partial void OnOffsetDarkChanged(int value)
    {
        if (_isInitializing)
            return;

        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }
}
