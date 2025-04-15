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
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class TimeViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private readonly ILocalSettingsService _localSettingsService;

    [ObservableProperty]
    private bool _isAutoThemeSwitchingEnabled;

    // TODO: replace bools with Enum
    [ObservableProperty]
    private bool _isCustomTimes;

    [ObservableProperty]
    private bool _isLocationTimes;

    [ObservableProperty]
    private bool _isCoordinateTimes;

    [ObservableProperty]
    private bool _isWindowsNightLight;

    [ObservableProperty]
    private string? _locationNextUpdateDateDescription;

    [ObservableProperty]
    private string? _locationBlockText;

    [ObservableProperty]
    private string? _lightTimeBlockText;

    [ObservableProperty]
    private string? _darkTimeBlockText;

    [ObservableProperty]
    private TimeSpan _timeLightStart;

    [ObservableProperty]
    private TimeSpan _timeDarkStart;

    [ObservableProperty]
    private string? _timePickHourClock;

    [ObservableProperty]
    private double _latValue;

    [ObservableProperty]
    private double _lonValue;

    [ObservableProperty]
    private Visibility _locationSettingsCardVisibility;

    [ObservableProperty]
    private Visibility _offsetTimeSettingsCardVisibility;

    [ObservableProperty]
    private int _offsetLight;

    [ObservableProperty]
    private int _offsetDark;

    public ICommand SaveOffsetCommand { get; }

    //TODO The logic part about Postpone is not written
    public TimeViewModel(IErrorService errorService, ILocalSettingsService localSettingsService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;
        _localSettingsService = localSettingsService;

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();

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
            await LoadGeolocationData();

            var twelveHourClock = await _localSettingsService.ReadSettingAsync<bool>("TwelveHourClock");
            if (!twelveHourClock)
            {
                TimePickHourClock = Windows.Globalization.ClockIdentifiers.TwentyFourHour;
                LocationNextUpdateDateDescription = "TimePageNextUpdateAt".GetLocalized() + nextUpdate.ToString(CultureInfo.CreateSpecificCulture("de"));
                LightTimeBlockText = "lblLight".GetLocalized() + ": " + SunriseWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture);
                DarkTimeBlockText = "lblDark".GetLocalized() + ": " + SunsetWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
            else
            {
                TimePickHourClock = Windows.Globalization.ClockIdentifiers.TwelveHour;
                LocationNextUpdateDateDescription = "TimePageNextUpdateAt".GetLocalized() + nextUpdate.ToString(CultureInfo.CreateSpecificCulture("en"));
                LightTimeBlockText = "lblLight".GetLocalized() + ": " + SunriseWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                DarkTimeBlockText = "lblDark".GetLocalized() + ": " + SunsetWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
        });

        ResetAllOptions();

        if (!_builder.Config.AutoThemeSwitchingEnabled)
        {
            HandleAutoThemeDisabled();
        }
        else
        {
            HandleAutoThemeEnabled();
        }
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

    private void ResetAllOptions()
    {
        IsCustomTimes = false;
        IsLocationTimes = false;
        IsCoordinateTimes = false;
        IsWindowsNightLight = false;
        LocationSettingsCardVisibility = Visibility.Collapsed;
        OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
    }

    private void HandleAutoThemeDisabled()
    {
        IsAutoThemeSwitchingEnabled = false;

        if (_builder.Config.Governor == Governor.NightLight)
        {
            IsWindowsNightLight = true;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            IsCustomTimes = true;
        }
        else if (_builder.Config.Location.UseGeolocatorService)
        {
            IsLocationTimes = true;
            LocationSettingsCardVisibility = Visibility.Visible;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
        }
        else
        {
            IsCoordinateTimes = true;
        }
    }

    private void HandleAutoThemeEnabled()
    {
        IsAutoThemeSwitchingEnabled = true;

        if (_builder.Config.Governor == Governor.NightLight)
        {
            IsWindowsNightLight = true;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            IsCustomTimes = true;
            return;
        }

        if (_builder.Config.Location.UseGeolocatorService)
        {
            IsLocationTimes = true;
            LocationSettingsCardVisibility = Visibility.Visible;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
        }
        else
        {
            IsCoordinateTimes = true;
            LocationSettingsCardVisibility = Visibility.Visible;
        }
    }

    private void SafeApplyTheme()
    {
        _dispatcherQueue?.TryEnqueue(async () => await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15));
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StateUpdateHandler.StopConfigWatcher();

            _builder.Load();
            LoadSettings();

            StateUpdateHandler.StartConfigWatcher();
        });
    }

    partial void OnIsAutoThemeSwitchingEnabledChanged(bool value)
    {
        if (value)
        {
            _builder.Config.AutoThemeSwitchingEnabled = true;
        }
        else
        {
            _builder.Config.AutoThemeSwitchingEnabled = false;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }
    }

    partial void OnIsCustomTimesChanged(bool value)
    {
        if (value)
        {
            _builder.Config.Governor = Governor.Default;
            _builder.Config.Location.Enabled = false;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
            }

            OffsetTimeSettingsCardVisibility = Visibility.Collapsed;

            SafeApplyTheme();
        }
    }

    partial void OnIsLocationTimesChanged(bool value)
    {
        if (value)
        {
            _builder.Config.Location.Enabled = true;
            _builder.Config.Location.UseGeolocatorService = true;
            _builder.Config.Governor = Governor.Default;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
            }

            OffsetTimeSettingsCardVisibility = Visibility.Visible;

            SafeApplyTheme();
        }
    }

    partial void OnIsCoordinateTimesChanged(bool value)
    {
        if (value)
        {
            _builder.Config.Governor = Governor.Default;
            _builder.Config.Location.Enabled = true;
            _builder.Config.Location.UseGeolocatorService = false;
            _builder.Config.Location.CustomLat = LatValue;
            _builder.Config.Location.CustomLon = LonValue;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
            }

            OffsetTimeSettingsCardVisibility = Visibility.Visible;

            SafeApplyTheme();
        }
    }

    partial void OnIsWindowsNightLightChanged(bool value)
    {
        if (value)
        {
            _builder.Config.Governor = Governor.NightLight;
            _builder.Config.AutoThemeSwitchingEnabled = true;
            _builder.Config.Location.Enabled = false;
            try
            {
                _builder.Save();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
            }

            OffsetTimeSettingsCardVisibility = Visibility.Collapsed;

            SafeApplyTheme();
        }
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }

        SafeApplyTheme();
    }

    partial void OnLatValueChanged(double value) => UpdateCoordinates();

    partial void OnLonValueChanged(double value) => UpdateCoordinates();

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
