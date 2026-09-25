namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    public enum SwitchTriggerMode
    {
        CustomTimes,
        LocationTimes,
        CoordinateTimes,
        WindowsNightLight,
        AmbientLight,
    }

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

    private void HandleAutoTheme(bool value, bool persist = true)
    {
        AutoThemeSwitchingEnabled = value;

        var mode = DetermineModeFromBackend();
        if (SelectedTriggerMode != mode) SelectedTriggerMode = mode;

        if (_isInitializing) ApplyTriggerModeState(mode);

        if (persist && _builder.Config.AutoThemeSwitchingEnabled != value)
            try
            {
                _builder.Config.AutoThemeSwitchingEnabled = value;
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
            }
    }

    private SwitchTriggerMode DetermineModeFromBackend()
    {
        switch (_builder.Config.Governor)
        {
            case Governor.NightLight:
                return SwitchTriggerMode.WindowsNightLight;
            case Governor.AmbientLight:
                return SwitchTriggerMode.AmbientLight;
            default:
                if (_builder.Config.Location.Enabled)
                {
                    return _builder.Config.Location.UseGeolocatorService ? SwitchTriggerMode.LocationTimes : SwitchTriggerMode.CoordinateTimes;
                }
                else
                {
                    return SwitchTriggerMode.CustomTimes;
                }
        }
    }

    private void ApplyTriggerModeState(SwitchTriggerMode mode)
    {
        // Base: reset all to default state
        // UI will not flicker
        _builder.Config.Location.Enabled = false;
        _builder.Config.Location.UseGeolocatorService = false;

        LocationSettingsCardVisibility = Visibility.Collapsed;
        CustomTimeSettingsCardVisibility = Visibility.Collapsed;
        OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
        PostponeOptionsSkipOnceVisibility = Visibility.Collapsed;
        OffsetTimesMinimum = -720;

        switch (mode)
        {
            case SwitchTriggerMode.CustomTimes:
                _builder.Config.Governor = Governor.Default;
                //_builder.Config.Location.Enabled = false;
                //_builder.Config.Location.UseGeolocatorService = false;

                //LocationSettingsCardVisibility = Visibility.Collapsed;
                CustomTimeSettingsCardVisibility = Visibility.Visible;
                //OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                //OffsetTimesMinimum = -720;
                //PostponeOptionsSkipOnceVisibility = Visibility.Collapsed;
                break;
            case SwitchTriggerMode.LocationTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = true;

                LocationSettingsCardVisibility = Visibility.Visible;
                CustomTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                //OffsetTimesMinimum = -720;
                PostponeOptionsSkipOnceVisibility = Visibility.Visible;
                break;
            case SwitchTriggerMode.CoordinateTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                //_builder.Config.Location.UseGeolocatorService = false;

                LocationSettingsCardVisibility = Visibility.Visible;
                CustomTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                //OffsetTimesMinimum = -720;
                PostponeOptionsSkipOnceVisibility = Visibility.Visible;
                break;
            case SwitchTriggerMode.WindowsNightLight:
                _builder.Config.Governor = Governor.NightLight;
                //_builder.Config.Location.Enabled = false;
                //_builder.Config.Location.UseGeolocatorService = false;

                //LocationSettingsCardVisibility = Visibility.Collapsed;
                //CustomTimeSettingsCardVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = 0;
                PostponeOptionsSkipOnceVisibility = Visibility.Visible;
                break;
            case SwitchTriggerMode.AmbientLight:
                _builder.Config.Governor = Governor.AmbientLight;
                //_builder.Config.Location.Enabled = false;
                //_builder.Config.Location.UseGeolocatorService = false;

                //LocationSettingsCardVisibility = Visibility.Collapsed;
                //CustomTimeSettingsCardVisibility = Visibility.Collapsed;
                //OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                OffsetTimesMinimum = 0;
                //PostponeOptionsSkipOnceVisibility = Visibility.Collapsed;
                break;
        }
    }

    partial void OnAutoThemeSwitchingEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        HandleAutoTheme(value);
    }

    partial void OnSelectedTriggerModeChanged(SwitchTriggerMode value)
    {
        if (_isInitializing)
            return;

        // Prevent flickering
        if (DetermineModeFromBackend() == value) return;

        ApplyTriggerModeState(value);

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
