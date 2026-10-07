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
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _sensorTimer;
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

        _sensorTimer = _dispatcherQueue.CreateTimer();
        _sensorTimer.Interval = TimeSpan.FromMilliseconds(500);
        _sensorTimer.Tick += (_, _) => RefreshAmbientReading();
        _sensorTimer.Start();
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

        RefreshAmbientReading();
        AmbientLightDarkThreshold = _builder.Config.AmbientLight.DarkThreshold;
        AmbientLightLightThreshold = _builder.Config.AmbientLight.LightThreshold;

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
        NotifyAutomationSwitches();
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
