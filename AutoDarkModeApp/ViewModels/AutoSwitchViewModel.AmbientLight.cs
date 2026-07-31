using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Ambient Light sensor and threshold management
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

    // Maximum lux value supported
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

    public Microsoft.UI.Xaml.GridLength GetStarWidth(double value)
    {
        return new Microsoft.UI.Xaml.GridLength(value, Microsoft.UI.Xaml.GridUnitType.Star);
    }

    private Windows.Devices.Sensors.LightSensor? _lightSensor;

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
