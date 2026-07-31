using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Core initialization, enums, and shared logic
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

    public enum PauseMode
    {
        Off,
        Once,
        Timed
    }

    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private readonly IGeolocatorService _geolocatorService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _debounceTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _ambientLightDebounceTimer;
    private bool _isInitializing;
    private bool _isUpdating;

    public AutoSwitchViewModel(IErrorService errorService, IGeolocatorService geolocatorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;
        _geolocatorService = geolocatorService;

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        LoadSettings();
        //Task.Run(() => LoadPauseTimer(null, new()));

        StateUpdateHandler.AddDebounceEventOnConfigUpdate(() => HandleConfigUpdate());
        StateUpdateHandler.StartConfigWatcher();

        // Don't call from background-thread, but via UI dispatcher
        StateUpdateHandler.OnPostponeTimerTick += (s, e) =>
        {
            _dispatcherQueue.TryEnqueue(() => LoadPauseTimer(s, e));
        };
        StateUpdateHandler.StartPostponeTimer();

        _debounceTimer = _dispatcherQueue.CreateTimer();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(500);
        _debounceTimer.Tick += (s, e) =>
        {
            _builder.Config.Location.SunriseOffsetMin = OffsetLight;
            _builder.Config.Location.SunsetOffsetMin = OffsetDark;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
            }
            _debounceTimer.Stop();

            SafeApplyTheme();
        };

        _ambientLightDebounceTimer = _dispatcherQueue.CreateTimer();
        _ambientLightDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
        _ambientLightDebounceTimer.Tick += (s, e) =>
        {
            _builder.Config.AmbientLight.DarkThreshold = AmbientLightDarkThreshold;
            _builder.Config.AmbientLight.LightThreshold = AmbientLightLightThreshold;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
            }
            _ambientLightDebounceTimer.Stop();

            // Trigger theme re-evaluation with new thresholds
            SafeApplyTheme();
        };
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        OffsetTimeSettingsCardVisibility = Visibility.Collapsed;

        // Check ambient light sensor availability and set up monitoring
        try
        {
            _lightSensor = Windows.Devices.Sensors.LightSensor.GetDefault();
            AmbientLightSensorAvailable = _lightSensor != null;
            if (_lightSensor != null)
            {
                // Set report interval to ~100ms for smooth UI updates (or sensor min if slower)
                _lightSensor.ReportInterval = Math.Max(_lightSensor.MinimumReportInterval, 100);
                _lightSensor.ReadingChanged += OnLightSensorReadingChanged;
                // Get initial reading
                var reading = _lightSensor.GetCurrentReading();
                if (reading != null)
                {
                    CurrentLuxReading = reading.IlluminanceInLux;
                    CurrentLuxDescription = GetLuxDescription(CurrentLuxReading);
                    CurrentLuxSliderPercentage = LogarithmicLuxConverter.LuxToSlider(CurrentLuxReading);
                    RemainingLuxSliderPercentage = 1000 - CurrentLuxSliderPercentage;
                }
            }
            else
            {
                CurrentLuxDescription = "AmbientLightNoSensor".GetLocalized();
            }
        }
        catch
        {
            AmbientLightSensorAvailable = false;
            CurrentLuxDescription = "AmbientLightNoSensor".GetLocalized();
        }

        // Load ambient light threshold settings
        AmbientLightDarkThreshold = _builder.Config.AmbientLight.DarkThreshold;
        AmbientLightLightThreshold = _builder.Config.AmbientLight.LightThreshold;

        HandleAutoTheme(_builder.Config.AutoThemeSwitchingEnabled);

        TimePickHourClock = Windows.Globalization.ClockIdentifiers.TwentyFourHour;
        OffsetLight = _builder.Config.Location.SunriseOffsetMin;
        OffsetDark = _builder.Config.Location.SunsetOffsetMin;
        LocationBlockText = "Msg_SearchLoc".GetLocalized();
        LatValue = _builder.Config.Location.CustomLat.ToString(CultureInfo.InvariantCulture);
        LonValue = _builder.Config.Location.CustomLon.ToString(CultureInfo.InvariantCulture);

        string timeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        TimePickHourClock = timeFormat.Contains('h') ? Windows.Globalization.ClockIdentifiers.TwelveHour : Windows.Globalization.ClockIdentifiers.TwentyFourHour;

        _dispatcherQueue.TryEnqueue(async () =>
        {
            // Only load geolocation data for location-based modes
            if (SelectedTriggerMode == SwitchTriggerMode.LocationTimes ||
                SelectedTriggerMode == SwitchTriggerMode.CoordinateTimes)
            {
                await LoadGeolocationData();

                LocationHandler.GetSunTimesWithOffset(_builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
                TimeLightStart = SunriseWithOffset.TimeOfDay;
                TimeDarkStart = SunsetWithOffset.TimeOfDay;
            }
            else if (SelectedTriggerMode == SwitchTriggerMode.CustomTimes)
            {
                TimeLightStart = _builder.Config.Sunrise.TimeOfDay;
                TimeDarkStart = _builder.Config.Sunset.TimeOfDay;
            }
            // AmbientLight and WindowsNightLight modes don't need time/location data
        });

        DateTime nextUpdate = _builder.LocationData.LastUpdate.Add(_builder.Config.Location.PollingCooldownTimeSpan);
        LocationNextUpdateDateDescription = "NextUpdateAt".GetLocalized() + ": " + nextUpdate.ToString("g", CultureInfo.CurrentCulture);

        if (SelectedTriggerMode == SwitchTriggerMode.AmbientLight)
        {
            PauseOptionsOnceVisibility = Visibility.Collapsed;

            if (SelectedPauseIndex == 1) // Once
                SelectedPauseIndex = 0; // Off
        }
        else
        {
            PauseOptionsOnceVisibility = Visibility.Visible;
        }

        _isInitializing = false;
    }

    private void HandleAutoTheme(bool value)
    {
        AutoThemeSwitchingEnabled = value;

        if (_builder.Config.Governor == Governor.NightLight)
        {
            SelectedTriggerMode = SwitchTriggerMode.WindowsNightLight;
            TimePickerVisibility = Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
            OffsetTimesMinimum = 0;
            return;
        }

        if (_builder.Config.Governor == Governor.AmbientLight)
        {
            SelectedTriggerMode = SwitchTriggerMode.AmbientLight;
            TimePickerVisibility = Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            SelectedTriggerMode = SwitchTriggerMode.CustomTimes;
            TimePickerVisibility = Visibility.Visible;
            return;
        }

        if (_builder.Config.Location.UseGeolocatorService)
        {
            SelectedTriggerMode = SwitchTriggerMode.LocationTimes;
        }
        else
        {
            SelectedTriggerMode = SwitchTriggerMode.CoordinateTimes;
        }

        OffsetTimesMinimum = -720;
        TimePickerVisibility = Visibility.Visible;
        OffsetTimeSettingsCardVisibility = Visibility.Visible;
    }

    private static async void SafeApplyTheme()
    {
        await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
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
