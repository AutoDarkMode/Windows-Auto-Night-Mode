using System.Globalization;
using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Utils;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp.ViewModels;

public partial class TimeViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private readonly IGeolocatorService _geolocatorService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _debounceTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _ambientLightDebounceTimer;
    private bool _isInitializing;
    private bool _isUpdating;

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
    public partial string? LocationNextUpdateDateDescription { get; set; }

    [ObservableProperty]
    public partial bool IsNoLocationAccessInfoBarOpen { get; set; }

    [ObservableProperty]
    public partial string? LocationBlockText { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeLightStart { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeDarkStart { get; set; }

    [ObservableProperty]
    public partial string? TimePickHourClock { get; set; }

    [ObservableProperty]
    public partial Visibility TimePickerVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility DividerBorderVisibility { get; set; }

    [ObservableProperty]
    public partial string? LatValue { get; set; }

    [ObservableProperty]
    public partial string? LonValue { get; set; }

    public ICommand SaveCoordinatesCommand { get; set; }

    [ObservableProperty]
    public partial Visibility OffsetTimeSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial int OffsetTimesMinimum { get; set; }

    [ObservableProperty]
    public partial int OffsetLight { get; set; }

    [ObservableProperty]
    public partial int OffsetDark { get; set; }

    [ObservableProperty]
    public partial bool IsPostponed { get; set; }

    [ObservableProperty]
    public partial int SelectedPostponeIndex { get; set; }

    [ObservableProperty]
    public partial string? PostponeInfoText { get; set; }

    [ObservableProperty]
    public partial bool ResumeInfoBarEnabled { get; set; }

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
                    // Ensure Light stays above Dark
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
                    // Ensure Dark stays below Light
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
                AmbientLightDarkThreshold = SliderToLux(value);
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
                AmbientLightLightThreshold = SliderToLux(value);
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

    public Microsoft.UI.Xaml.GridLength GetStarWidth(double value)
    {
        return new Microsoft.UI.Xaml.GridLength(value, Microsoft.UI.Xaml.GridUnitType.Star);
    }

    [ObservableProperty]
    public partial double CurrentLuxSliderPercentage { get; set; }

    [ObservableProperty]
    public partial double RemainingLuxSliderPercentage { get; set; } = 1000;

    [ObservableProperty]
    public partial bool AmbientLightSensorAvailable { get; set; }

    [ObservableProperty]
    public partial double CurrentLuxReading { get; set; }

    [ObservableProperty]
    public partial string? CurrentLuxDescription { get; set; }

    private Windows.Devices.Sensors.LightSensor? _lightSensor;

    public ICommand AutoConfigureCommand { get; }

    public TimeViewModel(IErrorService errorService, IGeolocatorService geolocatorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;
        _geolocatorService = geolocatorService;

        AutoConfigureCommand = new RelayCommand(AutoConfigure);

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }

        LoadSettings();
        Task.Run(() => LoadPostponeTimer(null, new()));

        StateUpdateHandler.AddDebounceEventOnConfigUpdate(() => HandleConfigUpdate());
        StateUpdateHandler.StartConfigWatcher();

        StateUpdateHandler.OnPostponeTimerTick += LoadPostponeTimer;
        StateUpdateHandler.StartPostponeTimer();

        SaveCoordinatesCommand = new RelayCommand(() =>
        {
            UpdateCoordinates();
        });

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
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
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
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
            }
            _ambientLightDebounceTimer.Stop();

            // Trigger theme re-evaluation with new thresholds
            SafeApplyTheme();
        };
    }

    private void AutoConfigure()
    {
        if (!AmbientLightSensorAvailable) return;

        double currentLux = CurrentLuxReading;
        double dark, light;

        // "Shifted Window" logic: anchor the threshold based on current active theme
        // If Light: Current lux is "Nominal Light", so set Light threshold slightly below it.
        // If Dark: Current lux is "Nominal Dark", so set Dark threshold slightly above it.
        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            light = Math.Max(1, currentLux * 0.9); // Anchor light threshold just below current
            dark = Math.Max(0, light / 3);         // Set dark threshold significantly lower (1:3 ratio)
        }
        else
        {
            dark = Math.Max(10, currentLux * 1.1);  // Anchor dark threshold just above current, min 10 lux
            light = Math.Max(dark + 2, dark * 3);  // Set light threshold significantly higher (1:3 ratio)
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
                SafeApplyTheme();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
            }
        }
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
            if (SelectedTriggerMode == SwitchTriggerMode.CustomTimes)
            {
                TimeLightStart = _builder.Config.Sunrise.TimeOfDay;
                TimeDarkStart = _builder.Config.Sunset.TimeOfDay;
            }
            else
            {
                await LoadGeolocationData();

                LocationHandler.GetSunTimesWithOffset(_builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
                TimeLightStart = SunriseWithOffset.TimeOfDay;
                TimeDarkStart = SunsetWithOffset.TimeOfDay;
            }
        });

        DateTime nextUpdate = _builder.LocationData.LastUpdate.Add(_builder.Config.Location.PollingCooldownTimeSpan);
        LocationNextUpdateDateDescription = "NextUpdateAt".GetLocalized() + ": " + nextUpdate.ToString("g", CultureInfo.CurrentCulture);

        _isInitializing = false;
    }

    private async Task LoadGeolocationData()
    {
        var maxTries = 5;
        for (var i = 0; i < maxTries; i++)
        {
            var result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GeolocatorIsUpdating));
            if (result.StatusCode == StatusCode.Ok)
            {
                break;
            }

            await Task.Delay(1000);
        }
        _builder.LoadLocationData();
        try
        {
            var result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.LocationAccess));
            if (_builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.NoLocAccess)
            {
                IsNoLocationAccessInfoBarOpen = true;
            }
            else if (_builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.Ok)
            {
                LocationBlockText = await _geolocatorService.GetRegionNameAsync(_builder.LocationData.Lon, _builder.LocationData.Lat);
            }
            else if (!_builder.Config.Location.UseGeolocatorService)
            {
                LocationBlockText = await _geolocatorService.GetRegionNameAsync(_builder.LocationData.Lon, _builder.LocationData.Lat);
            }
        }
        catch
        {
            return;
        }
    }

    private void HandleAutoTheme(bool value)
    {
        AutoThemeSwitchingEnabled = value;

        if (_builder.Config.Governor == Governor.NightLight)
        {
            SelectedTriggerMode = SwitchTriggerMode.WindowsNightLight;
            TimePickerVisibility = Visibility.Collapsed;
            DividerBorderVisibility = Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
            OffsetTimesMinimum = 0;
            return;
        }

        if (_builder.Config.Governor == Governor.AmbientLight)
        {
            SelectedTriggerMode = SwitchTriggerMode.AmbientLight;
            TimePickerVisibility = Visibility.Collapsed;
            DividerBorderVisibility = Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            SelectedTriggerMode = SwitchTriggerMode.CustomTimes;
            TimePickerVisibility = Visibility.Visible;
            DividerBorderVisibility = Visibility.Collapsed;
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
        DividerBorderVisibility = Visibility.Visible;
    }

    private void LoadPostponeTimer(object? sender, EventArgs e)
    {
        _isInitializing = true;

        ApiResponse reply = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.GetPostponeStatus));
        if (reply.StatusCode != StatusCode.Timeout)
        {
            if (_builder.Config.AutoThemeSwitchingEnabled)
            {
                try
                {
                    if (reply.Message == "True")
                    {
                        bool anyNoExpiry = false;
                        bool canResume = false;
                        PostponeQueueDto dto = PostponeQueueDto.Deserialize(reply.Details);
                        List<string> localizedItems = dto
                            .Items.Select(i =>
                            {
                                if (i.Expiry == null)
                                    anyNoExpiry = true;
                                if (i.IsUserClearable)
                                    canResume = true;

                                i.SetCulture(new CultureInfo(Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride));

                                return i.GetLocalizationData().BuildLocalizedString();
                            })
                            .ToList();

                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            _isInitializing = true;

                            ResumeInfoBarEnabled = anyNoExpiry && !canResume;
                            IsPostponed = canResume;
                            PostponeInfoText = "ActiveDelays".GetLocalized() + ": " + string.Join('\n', localizedItems);

                            _isInitializing = false;
                        });
                    }
                    else
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            IsPostponed = false;
                            PostponeInfoText = "ActiveDelays".GetLocalized() + ": " + "Msg_AutoSwitchEnabled".GetLocalized();
                            ResumeInfoBarEnabled = false;
                        });
                    }
                }
                catch { }
            }
        }

        _isInitializing = false;
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }
    }

    partial void OnSelectedTriggerModeChanged(SwitchTriggerMode value)
    {
        if (_isInitializing)
            return;

        // Each case fully controls all visibility states to prevent flickering
        switch (value)
        {
            case SwitchTriggerMode.CustomTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Visible;
                DividerBorderVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                break;

            case SwitchTriggerMode.LocationTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = true;
                TimePickerVisibility = Visibility.Visible;
                DividerBorderVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case SwitchTriggerMode.CoordinateTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Visible;
                DividerBorderVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case SwitchTriggerMode.WindowsNightLight:
                _builder.Config.Governor = Governor.NightLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Collapsed;
                DividerBorderVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = 0;
                break;

            case SwitchTriggerMode.AmbientLight:
                // Run auto-configure only if we are switching to Ambient Light and values are still defaults
                // This prevents overwriting user's custom settings when switching modes
                if (_builder.Config.AmbientLight.DarkThreshold == 40 && _builder.Config.AmbientLight.LightThreshold == 80)
                {
                    AutoConfigure();
                }
                _builder.Config.Governor = Governor.AmbientLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Collapsed;
                DividerBorderVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                break;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }

        SafeApplyTheme();
    }

    partial void OnTimeLightStartChanged(TimeSpan value)
    {
        if (_isInitializing || SelectedTriggerMode != SwitchTriggerMode.CustomTimes)
            return;

        _builder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, value.Hours, value.Minutes, 0);
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }

        SafeApplyTheme();
    }

    partial void OnTimeDarkStartChanged(TimeSpan value)
    {
        if (_isInitializing || SelectedTriggerMode != SwitchTriggerMode.CustomTimes)
            return;

        _builder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, value.Hours, value.Minutes, 0);
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }

        SafeApplyTheme();
    }

    private void UpdateCoordinates()
    {
        if (double.TryParse(LatValue!.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
        {
            if (lat > 90)
                lat = 90.000000;
            if (lat < -90)
                lat = -90.000000;

            LatValue = lat.ToString("0.######", CultureInfo.InvariantCulture);
        }
        else
        {
            LatValue = "0";
        }
        if (double.TryParse(LonValue!.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
        {
            if (lon > 180)
                lon = 180.000000;
            if (lon < -180)
                lon = -180.000000;

            LonValue = lon.ToString("0.######", CultureInfo.InvariantCulture);
        }
        else
        {
            LonValue = "0";
        }

        _builder.Config.Location.CustomLat = lat;
        _builder.Config.Location.CustomLon = lon;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimeViewModel");
        }

        SafeApplyTheme();
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

    partial void OnIsPostponedChanged(bool value)
    {
        if (_isInitializing)
            return;

        var postponeMinutes = (SelectedPostponeIndex) switch
        {
            0 => 15,
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 180,
            5 => 360,
            6 => 720,
            7 => 0,
            _ => 0,
        };

        if (postponeMinutes != 0 && value)
        {
            MessageHandler.Client.SendMessageAndGetReply($"{Command.DelayBy} {postponeMinutes}");
        }
        else if (postponeMinutes == 0 && value)
        {
            MessageHandler.Client.SendMessageAndGetReply(Command.ToggleSkipNext);
            if (!value)
                MessageHandler.Client.SendMessageAndGetReply(Command.RequestSwitch);
        }
        else
        {
            MessageHandler.Client.SendMessageAndGetReply(Command.ClearPostponeQueue);
            MessageHandler.Client.SendMessageAndGetReply(Command.RequestSwitch);
        }

        LoadPostponeTimer(null, new());
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
