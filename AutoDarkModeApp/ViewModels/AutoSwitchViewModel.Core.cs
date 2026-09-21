using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
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
        Task.Run(() => LoadPostponeTimer(null, new()));

        StateUpdateHandler.AddDebounceEventOnConfigUpdate(HandleConfigUpdate);
        StateUpdateHandler.StartConfigWatcher();

        StateUpdateHandler.OnPostponeTimerTick += LoadPostponeTimer;
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
            RequestThemeSwitch();
        };
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        // Check ambient light sensor availability and set up monitoring
        _lightSensor = Windows.Devices.Sensors.LightSensor.GetDefault();
        AmbientLightSensorAvailable = _lightSensor != null;

        if (AmbientLightSensorAvailable)
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
            else
            {
                CurrentLuxReading = 0;
                CurrentLuxDescription = "AmbientLightNoReading".GetLocalized();
                CurrentLuxSliderPercentage = 0;
                RemainingLuxSliderPercentage = 1000;
            }

            // Load ambient light threshold settings
            AmbientLightDarkThreshold = _builder.Config.AmbientLight.DarkThreshold;
            AmbientLightLightThreshold = _builder.Config.AmbientLight.LightThreshold;
        }
        else
        {
            // No sensor available - show helpful text but continue initializing other settings
            CurrentLuxDescription = "AmbientLightNoSensor".GetLocalized();
        }

        HandleAutoTheme(_builder.Config.AutoThemeSwitchingEnabled);

        LatValue = _builder.Config.Location.CustomLat.ToString(CultureInfo.InvariantCulture);
        LonValue = _builder.Config.Location.CustomLon.ToString(CultureInfo.InvariantCulture);

        LocationBlockText = "Msg_SearchLoc".GetLocalized();

        OffsetLight = _builder.Config.Location.SunriseOffsetMin;
        OffsetDark = _builder.Config.Location.SunsetOffsetMin;

        _dispatcherQueue.TryEnqueue(async () =>
                {
                    switch (SelectedTriggerMode)
                    {
                        // Only load geolocation data for location-based modes
                        // AmbientLight and WindowsNightLight modes don't need time/location data
                        case SwitchTriggerMode.LocationTimes:
                        case SwitchTriggerMode.CoordinateTimes:
                        {
                            await LoadGeolocationData();

                            LocationHandler.GetSunTimesWithOffset(_builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
                            TimeLightStart = SunriseWithOffset.TimeOfDay;
                            TimeDarkStart = SunsetWithOffset.TimeOfDay;

                            // location data has been reloaded from disk by now, so the next update time may have become available
                            UpdateLocationNextUpdateDescription();
                            break;
                        }

                        case SwitchTriggerMode.CustomTimes:
                            TimeLightStart = _builder.Config.Sunrise.TimeOfDay;
                            TimeDarkStart = _builder.Config.Sunset.TimeOfDay;
                            break;
                    }
                });

        UpdateLocationNextUpdateDescription();

        _isInitializing = false;
    }

    private static async void RequestThemeSwitch()
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
}
