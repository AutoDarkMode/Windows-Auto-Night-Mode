using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private readonly IThemeSwitchService _themeSwitchService;
    private readonly IGeolocatorService _geolocatorService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _debounceTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _ambientLightDebounceTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _webcamPreviewTimer;
    private bool _isInitializing;
    private bool _isUpdating;
    private bool _locationDataInitialized;
    private int _locationAccessRefreshVersion;

    public AutoSwitchViewModel(IErrorService errorService, IGeolocatorService geolocatorService, IThemeSwitchService themeSwitchService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;
        _geolocatorService = geolocatorService;
        _themeSwitchService = themeSwitchService;

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

        // Watches location_data.yaml directly so background service updates (hourly timer / 24h cooldown)
        // are reflected on this page immediately, without requiring an unrelated config.yaml save. See #1078.
        StateUpdateHandler.AddDebounceEventOnLocationDataUpdate(HandleLocationDataUpdate);
        StateUpdateHandler.StartLocationDataWatcher();

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
            _builder.Config.AmbientLight.WebcamIntervalSec = WebcamInterval;
            _builder.Config.AmbientLight.WeatherIntervalMin = WeatherInterval;
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
            _ = _themeSwitchService.RequestThemeSwitchAsync(GetType().Name, App.MainWindow.Content.XamlRoot);
        };

        // Updates the current light level about once per second while the webcam preview is on
        _webcamPreviewTimer = _dispatcherQueue.CreateTimer();
        _webcamPreviewTimer.Interval = TimeSpan.FromSeconds(1);
        _webcamPreviewTimer.Tick += (s, e) =>
        {
            if (_webcamPreview == null) return;
            try
            {
                using var frame = _webcamPreview.Reader.TryAcquireLatestFrame();
                double? lux = AmbientLightEstimator.EstimateWebcamLux(_webcamPreview, frame);
                if (lux != null) SetCurrentLux(lux);
            }
            catch (Exception)
            {
                // e.g. the webcam was unplugged
                StopWebcamPreviewWithError(AmbientLightMessage.WebcamUnavailable);
            }
        };
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        // Theme mode
        HandleAutoTheme(_builder.Config.AutoThemeSwitchingEnabled, persist: false);

        // Coordinates
        LatValue = _builder.Config.Location.CustomLat.ToString(CultureInfo.InvariantCulture);
        LonValue = _builder.Config.Location.CustomLon.ToString(CultureInfo.InvariantCulture);

        // Location UI
        if (!_locationDataInitialized) LocationBlockText = "Msg_SearchLoc".GetLocalized();

        // Offsets
        OffsetLight = _builder.Config.Location.SunriseOffsetMin;
        OffsetDark = _builder.Config.Location.SunsetOffsetMin;

        // Trigger mode
        _dispatcherQueue.TryEnqueue(async () =>
                {
                    switch (SelectedTriggerMode)
                    {
                        // Only load geolocation data for location-based modes
                        // AmbientLight and WindowsNightLight modes don't need time/location data
                        case SwitchTriggerMode.LocationTimes:
                        case SwitchTriggerMode.CoordinateTimes:
                        {
                            if (!_locationDataInitialized)
                            {
                                // Poll the service for geolocator/access status only on true first load
                                // (app startup / initial page open). Later LoadSettings() calls are
                                // triggered by unrelated config saves (trigger mode, offsets, ...) and must
                                // not re-poll the location service every time - see #1078 investigation.
                                await LoadGeolocationData();
                            }

                            LocationHandler.GetSunTimesWithOffset(_builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
                            TimeLightStart = SunriseWithOffset.TimeOfDay;
                            TimeDarkStart = SunsetWithOffset.TimeOfDay;

                            // location data has been reloaded from disk by now, so the next update time may have become available
                            // UpdateLocationNextUpdateDescription();
                            break;
                        }

                        case SwitchTriggerMode.CustomTimes:
                            TimeLightStart = _builder.Config.Sunrise.TimeOfDay;
                            TimeDarkStart = _builder.Config.Sunset.TimeOfDay;
                            break;
                    }
                });

        // Next update time
        UpdateLocationNextUpdateDescription();

        // Ambient light sensor availability and monitoring
        _lightSensor = Windows.Devices.Sensors.LightSensor.GetDefault();
        AmbientLightSensorAvailable = _lightSensor != null;

        if (AmbientLightSensorAvailable)
        {
            // Set report interval to ~100ms for smooth UI updates (or sensor min if slower)
            _lightSensor.ReportInterval = Math.Max(_lightSensor.MinimumReportInterval, 100);
            _lightSensor.ReadingChanged += OnLightSensorReadingChanged;
        }

        // Load ambient light settings
        AmbientLightDarkThreshold = _builder.Config.AmbientLight.DarkThreshold;
        AmbientLightLightThreshold = _builder.Config.AmbientLight.LightThreshold;
        SelectedAmbientLightSource = _builder.Config.AmbientLight.Source;
        WebcamInterval = _builder.Config.AmbientLight.WebcamIntervalSec;
        WeatherInterval = _builder.Config.AmbientLight.WeatherIntervalMin;
        WeatherUseLocationService = _builder.Config.Location.UseGeolocatorService;
        _dispatcherQueue.TryEnqueue(async () => await LoadWebcamsAsync());

        _isInitializing = false;

        ShowCurrentLux();

        // OnSelectedTriggerModeChanged skips the access check while initializing, and LoadGeolocationData only
        // runs on first load, so a config reload into a mode using the location service has to refresh the info bar here
        bool isAwaitingFirstLocationLoad = SelectedTriggerMode == SwitchTriggerMode.LocationTimes && !_locationDataInitialized;
        bool needsLocationAccessCheck = UsesLocationService && !isAwaitingFirstLocationLoad;
        if (needsLocationAccessCheck) QueueLocationAccessInfoBarRefresh();
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

    /// <summary>
    /// Fired when the service writes new geoposition data in the background (location_data.yaml changed),
    /// independently of any config.yaml save. Refreshes the cached location display so the AutoSwitch page
    /// reflects background updates without requiring the user to trigger an unrelated config save. Fixes #1078.
    /// </summary>
    private void HandleLocationDataUpdate()
    {
        StateUpdateHandler.StopLocationDataWatcher();
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await RefreshLocationDisplay();
        });
        StateUpdateHandler.StartLocationDataWatcher();
    }
}
