using System.Globalization;
using System.Windows.Input;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
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

    //TODO: Temporary reservation
    private readonly ILocalSettingsService _localSettingsService;

    public enum TimeSourceMode
    {
        CustomTimes,
        LocationTimes,
        CoordinateTimes,
        WindowsNightLight,
    }

    [ObservableProperty]
    public partial bool AutoThemeSwitchingEnabled { get; set; }

    [ObservableProperty]
    public partial TimeSourceMode SelectedTimeSource { get; set; }

    [ObservableProperty]
    public partial string? LocationNextUpdateDateDescription { get; set; }

    [ObservableProperty]
    public partial string? LocationBlockText { get; set; }

    [ObservableProperty]
    public partial string? LightTimeBlockText { get; set; }

    [ObservableProperty]
    public partial string? DarkTimeBlockText { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeLightStart { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeDarkStart { get; set; }

    [ObservableProperty]
    public partial string? TimePickHourClock { get; set; }

    [ObservableProperty]
    public partial double LatValue { get; set; }

    [ObservableProperty]
    public partial double LonValue { get; set; }

    [ObservableProperty]
    public partial Visibility LocationSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility OffsetTimeSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial int OffsetLight { get; set; }

    [ObservableProperty]
    public partial int OffsetDark { get; set; }

    public ICommand SaveOffsetCommand { get; }

    //TODO: The logic part about Postpone is not written
    public TimeViewModel(IErrorService errorService, ILocalSettingsService localSettingsService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;
        //TODO: Temporary reservation
        _localSettingsService = localSettingsService;

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

        StateUpdateHandler.StartConfigWatcherWithoutEvents();
        StateUpdateHandler.AddDebounceEventOnConfigUpdate(() => HandleConfigUpdate());

        SaveOffsetCommand = new RelayCommand(() =>
        {
            _builder.Config.Location.SunriseOffsetMin = OffsetLight;
            _builder.Config.Location.SunsetOffsetMin = OffsetDark;
            _builder.Save();
        });
    }

    private void LoadSettings()
    {
        TimeLightStart = _builder.Config.Sunrise.TimeOfDay;
        TimeDarkStart = _builder.Config.Sunset.TimeOfDay;
        TimePickHourClock = Windows.Globalization.ClockIdentifiers.TwentyFourHour;
        OffsetLight = _builder.Config.Location.SunriseOffsetMin;
        OffsetDark = _builder.Config.Location.SunsetOffsetMin;
        LatValue = _builder.Config.Location.CustomLat;
        LonValue = _builder.Config.Location.CustomLon;

        LocationBlockText = "msgSearchLoc".GetLocalized();
        DateTime nextUpdate = _builder.LocationData.LastUpdate.Add(_builder.Config.Location.PollingCooldownTimeSpan);
        LocationHandler.GetSunTimesWithOffset(_builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
        _dispatcherQueue.TryEnqueue(async () =>
        {
            //TODO: A/B Testing. Let the time format completely follow the system settings
            await LoadGeolocationData();

            string timeFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;

            LightTimeBlockText = "lblLight".GetLocalized() + ": " + SunriseWithOffset.ToString("t", CultureInfo.CurrentCulture);
            DarkTimeBlockText = "lblDark".GetLocalized() + ": " + SunsetWithOffset.ToString("t", CultureInfo.CurrentCulture);

            LocationNextUpdateDateDescription = "TimePageNextUpdateAt".GetLocalized() + nextUpdate.ToString("g", CultureInfo.CurrentCulture);

            bool isSystem12HourFormat = timeFormat.Contains('h');
            TimePickHourClock = isSystem12HourFormat ? Windows.Globalization.ClockIdentifiers.TwelveHour : Windows.Globalization.ClockIdentifiers.TwentyFourHour;
        });

        LocationSettingsCardVisibility = Visibility.Collapsed;
        OffsetTimeSettingsCardVisibility = Visibility.Collapsed;

        HandleAutoTheme(_builder.Config.AutoThemeSwitchingEnabled);
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
            if (_builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.Ok)
            {
                LocationBlockText = "lblCity".GetLocalized() + ": " + await LocationHandler.GetCityName();
            }
            else if (!_builder.Config.Location.UseGeolocatorService)
            {
                LocationBlockText =
                    "lblPosition".GetLocalized()
                    + ": "
                    + "TimeNumberBoxHeaderLat".GetLocalized()
                    + " "
                    + Math.Round(_builder.LocationData.Lat, 3)
                    + " / "
                    + "TimeNumberBoxHeaderLon".GetLocalized()
                    + " "
                    + Math.Round(_builder.LocationData.Lon, 3);
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
            SelectedTimeSource = TimeSourceMode.WindowsNightLight;
            OffsetTimeSettingsCardVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            SelectedTimeSource = TimeSourceMode.CustomTimes;
            return;
        }

        if (_builder.Config.Location.UseGeolocatorService)
        {
            SelectedTimeSource = TimeSourceMode.LocationTimes;
            LocationSettingsCardVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            SelectedTimeSource = TimeSourceMode.CoordinateTimes;
            LocationSettingsCardVisibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SafeApplyTheme()
    {
        _dispatcherQueue.TryEnqueue(async () => await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15));
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
        HandleAutoTheme(value);

        _builder.Config.AutoThemeSwitchingEnabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }
    }

    partial void OnSelectedTimeSourceChanged(TimeSourceMode value)
    {
        switch (value)
        {
            case TimeSourceMode.CustomTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = false;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                break;

            case TimeSourceMode.LocationTimes:
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = true;
                _builder.Config.Governor = Governor.Default;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                break;

            case TimeSourceMode.CoordinateTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = false;
                _builder.Config.Location.CustomLat = LatValue;
                _builder.Config.Location.CustomLon = LonValue;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                break;

            case TimeSourceMode.WindowsNightLight:
                _builder.Config.Governor = Governor.NightLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
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
        _builder.Config.Location.CustomLat = LatValue;
        _builder.Config.Location.CustomLon = LonValue;
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

    partial void OnLatValueChanged(double value) => UpdateCoordinates();

    partial void OnLonValueChanged(double value) => UpdateCoordinates();

}
