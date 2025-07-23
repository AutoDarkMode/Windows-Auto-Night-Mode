﻿using System.Globalization;
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
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _debounceTimer;
    private bool _isInitializing;

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

    public TimeViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

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

        SaveCoordinatesCommand = new RelayCommand(UpdateCoordinates);

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
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        OffsetTimeSettingsCardVisibility = Visibility.Collapsed;

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
            if (SelectedTimeSource == TimeSourceMode.CustomTimes)
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
        LocationNextUpdateDateDescription = "NextUpdateAt".GetLocalized() + nextUpdate.ToString("g", CultureInfo.CurrentCulture);

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
            if (_builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.Ok)
            {
                LocationBlockText = "City".GetLocalized() + ": " + await LocationHandler.GetCityName();
            }
            else if (!_builder.Config.Location.UseGeolocatorService)
            {
                LocationBlockText =
                    "Position".GetLocalized()
                    + ": "
                    + "Latitude".GetLocalized()
                    + " "
                    + Math.Round(_builder.LocationData.Lat, 3)
                    + " / "
                    + "Longitude".GetLocalized()
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
            TimePickerVisibility = Visibility.Collapsed;
            DividerBorderVisibility = Visibility.Collapsed;
            OffsetTimeSettingsCardVisibility = Visibility.Visible;
            OffsetTimesMinimum = 0;
            return;
        }

        if (!_builder.Config.Location.Enabled)
        {
            SelectedTimeSource = TimeSourceMode.CustomTimes;
            TimePickerVisibility = Visibility.Visible;
            DividerBorderVisibility = Visibility.Collapsed;
            return;
        }

        if (_builder.Config.Location.UseGeolocatorService)
        {
            SelectedTimeSource = TimeSourceMode.LocationTimes;
        }
        else
        {
            SelectedTimeSource = TimeSourceMode.CoordinateTimes;
        }

        OffsetTimeSettingsCardVisibility = value ? Visibility.Visible : Visibility.Collapsed;
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

    partial void OnSelectedTimeSourceChanged(TimeSourceMode value)
    {
        if (_isInitializing)
            return;

        HandleAutoTheme(AutoThemeSwitchingEnabled);

        switch (value)
        {
            case TimeSourceMode.CustomTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                break;

            case TimeSourceMode.LocationTimes:
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = true;
                _builder.Config.Governor = Governor.Default;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case TimeSourceMode.CoordinateTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = false;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case TimeSourceMode.WindowsNightLight:
                _builder.Config.Governor = Governor.NightLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = 0;
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
        if (_isInitializing || SelectedTimeSource != TimeSourceMode.CustomTimes)
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
        if (_isInitializing || SelectedTimeSource != TimeSourceMode.CustomTimes)
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
}
