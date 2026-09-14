namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    private double _ambientLightDarkThreshold;
    public double AmbientLightDarkThreshold
    {
        get => _ambientLightDarkThreshold;
        set
        {
            value = Math.Round(value);
            if (SetProperty(ref _ambientLightDarkThreshold, value))
            {
                if (!_isUpdating)
                {
                    _isUpdating = true;
                    // Ensure Light stays strictly above Dark (not equal).
                    // While the RangeSelector control allows thumbs to overlap at the same value,
                    // having identical thresholds creates an ambiguous zone where it's unclear
                    // which theme should apply. Enforcing a minimum gap of 1 lux provides clear
                    // hysteresis for the theme switching logic.
                    if (_ambientLightLightThreshold <= value)
                    {
                        AmbientLightLightThreshold = Math.Min(10000, value + 1);
                    }
                    RangeStart = LuxToSlider(value);
                    _isUpdating = false;
                }
                RestartAmbientLightDebounce();
            }
        }
    }

    private double _ambientLightLightThreshold;
    public double AmbientLightLightThreshold
    {
        get => _ambientLightLightThreshold;
        set
        {
            value = Math.Round(value);
            if (SetProperty(ref _ambientLightLightThreshold, value))
            {
                if (!_isUpdating)
                {
                    _isUpdating = true;
                    // Ensure Dark stays strictly below Light (see comment in AmbientLightDarkThreshold)
                    if (_ambientLightDarkThreshold >= value)
                    {
                        AmbientLightDarkThreshold = Math.Max(1, value - 1);
                    }
                    RangeEnd = LuxToSlider(value);
                    _isUpdating = false;
                }
                RestartAmbientLightDebounce();
            }
        }
    }

    private void RestartAmbientLightDebounce()
    {
        if (_ambientLightDebounceTimer != null)
        {
            _ambientLightDebounceTimer.Stop();
            _ambientLightDebounceTimer.Start();
        }
    }

    private double _rangeStart;
    public double RangeStart
    {
        get => _rangeStart;
        set
        {
            if (SetProperty(ref _rangeStart, value) && !_isUpdating)
            {
                _isUpdating = true;
                double lux = SliderToLux(value);
                AmbientLightDarkThreshold = lux;

                // Snap slider to canonical position for rounded lux value
                // This ensures the thumb position matches the displayed value
                double canonicalSlider = LuxToSlider(lux);
                if (Math.Abs(_rangeStart - canonicalSlider) > 0.5)
                {
                    SetProperty(ref _rangeStart, canonicalSlider);
                }
                _isUpdating = false;
            }
        }
    }

    private double _rangeEnd;
    public double RangeEnd
    {
        get => _rangeEnd;
        set
        {
            if (SetProperty(ref _rangeEnd, value) && !_isUpdating)
            {
                _isUpdating = true;
                double lux = SliderToLux(value);
                AmbientLightLightThreshold = lux;

                // Snap slider to canonical position for rounded lux value
                // This ensures the thumb position matches the displayed value
                double canonicalSlider = LuxToSlider(lux);
                if (Math.Abs(_rangeEnd - canonicalSlider) > 0.5)
                {
                    SetProperty(ref _rangeEnd, canonicalSlider);
                }
                _isUpdating = false;
            }
        }
    }

    // Maximum lux value supported (matching my previous logic)
    private const double MaxLuxValue = 10000.0;
    // Slider range (0-1000 for finer precision)
    private const double SliderMaxValue = 1000.0;
    // Precomputed log constant
    private static readonly double LogBase = Math.Log(MaxLuxValue + 1);

    public double SliderToLux(double sliderValue)
    {
        if (sliderValue <= 0) return 0.0;
        if (sliderValue >= SliderMaxValue) return MaxLuxValue;
        double lux = Math.Exp(sliderValue / SliderMaxValue * LogBase) - 1;

        if (lux < 100) return Math.Round(lux);
        if (lux < 1000) return Math.Round(lux / 5) * 5;
        return Math.Round(lux / 10) * 10;
    }

    public double LuxToSlider(double lux)
    {
        if (lux <= 0) return 0.0;
        if (lux >= MaxLuxValue) return SliderMaxValue;
        return Math.Log(lux + 1) / LogBase * SliderMaxValue;
    }

    public static Microsoft.UI.Xaml.GridLength GetStarWidth(double value) => new GridLength(value, GridUnitType.Star);

    [ObservableProperty]
    public partial double CurrentLuxSliderPercentage { get; set; }

    [ObservableProperty]
    public partial double RemainingLuxSliderPercentage { get; set; } = 1000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmbientLightSensorTooltip))]
    public partial bool AmbientLightSensorAvailable { get; set; }

    public string AmbientLightSensorTooltip => AmbientLightSensorAvailable
        ? "AmbientLightSensor_ToolTip".GetLocalized()
        : "AmbientLightSensor_Unavailable_ToolTip".GetLocalized();

    [ObservableProperty]
    public partial double CurrentLuxReading { get; set; }

    [ObservableProperty]
    public partial string? CurrentLuxDescription { get; set; }

    private Windows.Devices.Sensors.LightSensor? _lightSensor;

    [RelayCommand]
    private void AutoConfigure()
    {
        if (!AmbientLightSensorAvailable) return;

        double currentLux = CurrentLuxReading;
        double dark, light;

        // Calculate gap using exponential scaling: smaller lux values get smaller gaps,
        // larger values get proportionally larger gaps (non-linear growth)
        // Examples: 10 lux → 5 gap, 41 lux → 13 gap, 100 lux → 25 gap, 1000 lux → 126 gap
        double gap = Math.Pow(Math.Max(1, currentLux), 0.7);

        // Anchor threshold based on current active theme
        switch (Application.Current.RequestedTheme)
        {
            case ApplicationTheme.Light:
                // Light theme: current lux is "nominal light", anchor light threshold near it
                light = Math.Max(1, currentLux * 0.95);
                dark = Math.Max(1, light - gap);
                break;
            default:
                // Dark theme: current lux is "nominal dark", anchor dark threshold near it
                dark = Math.Max(1, currentLux * 1.05);
                light = dark + gap;
                break;
        }

        // Clamp to valid range
        AmbientLightDarkThreshold = Math.Max(1, Math.Min(dark, 9998));
        AmbientLightLightThreshold = Math.Max(AmbientLightDarkThreshold + 1, Math.Min(light, 10000));

        // Save immediately as this is a deliberate action or first-time setup
        if (_ambientLightDebounceTimer != null)
        {
            _ambientLightDebounceTimer.Stop();
            _builder.Config.AmbientLight.DarkThreshold = AmbientLightDarkThreshold;
            _builder.Config.AmbientLight.LightThreshold = AmbientLightLightThreshold;
            try
            {
                _builder.Save();
                RequestThemeSwitch();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
            }
        }
    }

    private void OnLightSensorReadingChanged(Windows.Devices.Sensors.LightSensor sender, Windows.Devices.Sensors.LightSensorReadingChangedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            CurrentLuxReading = args.Reading.IlluminanceInLux;
            CurrentLuxDescription = GetLuxDescription(CurrentLuxReading);
            CurrentLuxSliderPercentage = LogarithmicLuxConverter.LuxToSlider(CurrentLuxReading);
            RemainingLuxSliderPercentage = 1000 - CurrentLuxSliderPercentage;
        });
    }

    private static string GetLuxDescription(double lux)
    {
        return lux switch
        {
            < 1 => $"{lux:F0} lux — Moonlight",
            < 10 => $"{lux:F0} lux — Very dark",
            < 50 => $"{lux:F0} lux — Dimly lit room",
            < 150 => $"{lux:F0} lux — Living room",
            < 400 => $"{lux:F0} lux — Office lighting",
            < 1000 => $"{lux:F0} lux — Overcast day",
            < 10000 => $"{lux:F0} lux — Daylight (shade)",
            < 30000 => $"{lux:F0} lux — Full daylight",
            _ => $"{lux:F0} lux — Direct sunlight"
        };
    }
}
