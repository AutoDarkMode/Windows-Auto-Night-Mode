namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel
{
    public bool AmbientThemeSwitchingEnabled
    {
        get => AutoThemeSwitchingEnabled && SelectedTriggerMode == SwitchTriggerMode.AmbientLight;
        set
        {
            if (value == AmbientThemeSwitchingEnabled) return;
            if (value)
            {
                if (SelectedTriggerMode != SwitchTriggerMode.AmbientLight)
                    _builder.Config.AmbientLight.PreviousTriggerMode = SelectedTriggerMode.ToString();
                SelectedTriggerMode = SwitchTriggerMode.AmbientLight;
                AutoThemeSwitchingEnabled = true;
            }
            else if (SelectedTriggerMode == SwitchTriggerMode.AmbientLight) AutoThemeSwitchingEnabled = false;
            NotifyAutomationSwitches();
        }
    }

    public bool TimeThemeSwitchingEnabled
    {
        get => AutoThemeSwitchingEnabled && SelectedTriggerMode != SwitchTriggerMode.AmbientLight;
        set
        {
            if (value == TimeThemeSwitchingEnabled) return;
            if (value && SelectedTriggerMode == SwitchTriggerMode.AmbientLight)
                SelectedTriggerMode = Enum.TryParse<SwitchTriggerMode>(_builder.Config.AmbientLight.PreviousTriggerMode, out var previous)
                    && previous != SwitchTriggerMode.AmbientLight && Enum.IsDefined(previous) ? previous : _builder.Config.Location.Enabled
                    ? (_builder.Config.Location.UseGeolocatorService ? SwitchTriggerMode.LocationTimes : SwitchTriggerMode.CoordinateTimes)
                    : SwitchTriggerMode.CustomTimes;
            AutoThemeSwitchingEnabled = value;
            NotifyAutomationSwitches();
        }
    }

    private void NotifyAutomationSwitches()
    {
        OnPropertyChanged(nameof(AmbientThemeSwitchingEnabled));
        OnPropertyChanged(nameof(TimeThemeSwitchingEnabled));
    }

    [ObservableProperty]
    public partial bool HasCurrentLuxReading { get; set; }

    // A shared UI timer discovers sensors again after unplugging/reflashing and never
    // accumulates ReadingChanged subscriptions when configuration is reloaded.
    private void RefreshAmbientReading()
    {
        try
        {
            var sensor = Windows.Devices.Sensors.LightSensor.GetDefault();
            AmbientLightSensorAvailable = sensor != null;
            if (sensor != null) sensor.ReportInterval = Math.Max(sensor.MinimumReportInterval, 250);
            var reading = sensor?.GetCurrentReading();
            HasCurrentLuxReading = reading != null && double.IsFinite(reading.IlluminanceInLux) && reading.IlluminanceInLux >= 0;
            if (HasCurrentLuxReading)
            {
                CurrentLuxReading = reading!.IlluminanceInLux;
                CurrentLuxDescription = GetLuxDescription(CurrentLuxReading);
                CurrentLuxSliderPercentage = LogarithmicLuxConverter.LuxToSlider(CurrentLuxReading);
                RemainingLuxSliderPercentage = 1000 - CurrentLuxSliderPercentage;
            }
            else CurrentLuxDescription = (AmbientLightSensorAvailable ? "AmbientLightNoReading" : "AmbientLightNoSensor").GetLocalized();
        }
        catch
        {
            AmbientLightSensorAvailable = false;
            HasCurrentLuxReading = false;
            CurrentLuxDescription = "AmbientLightNoSensor".GetLocalized();
        }
    }
}
