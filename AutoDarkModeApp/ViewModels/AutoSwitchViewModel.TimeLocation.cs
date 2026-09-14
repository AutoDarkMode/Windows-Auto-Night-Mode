using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
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
    public partial Visibility CustomTimeSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial Visibility LocationSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial string? LatValue { get; set; }

    [ObservableProperty]
    public partial string? LonValue { get; set; }

    [RelayCommand]
    private void SaveCoordinates()
    {
        if (double.TryParse(LatValue!.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
        {
            if (lat > 90) lat = 90.000000;
            if (lat < -90) lat = -90.000000;

            LatValue = lat.ToString("0.######", CultureInfo.InvariantCulture);
        }
        else
        {
            LatValue = "0";
        }

        if (double.TryParse(LonValue!.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
        {
            if (lon > 180) lon = 180.000000;
            if (lon < -180) lon = -180.000000;

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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        RequestThemeSwitch();
    }

    private void UpdateLocationNextUpdateDescription()
    {
        // the service has never written location data, so there is nothing to base a next update time on yet
        if (_builder.LocationData.LastUpdate == default)
        {
            LocationNextUpdateDateDescription = "NextUpdateAt".GetLocalized() + ": " + "NextUpdateInProgress".GetLocalized();
            return;
        }

        DateTime nextUpdate = _builder.LocationData.LastUpdate.Add(_builder.Config.Location.PollingCooldownTimeSpan);
        LocationNextUpdateDateDescription = "NextUpdateAt".GetLocalized() + ": " + nextUpdate.ToString("g", CultureInfo.CurrentCulture);
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
                LocationBlockText = "Msg_LocPerm".GetLocalized();
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

    partial void OnTimeLightStartChanged(TimeSpan value)
    {
        UpdateCustomTime(value, dt => _builder.Config.Sunrise = dt);
    }

    partial void OnTimeDarkStartChanged(TimeSpan value)
    {
        UpdateCustomTime(value, dt => _builder.Config.Sunset = dt);
    }

    private void UpdateCustomTime(TimeSpan value, Action<DateTime> setConfig)
    {
        if (_isInitializing || SelectedTriggerMode != SwitchTriggerMode.CustomTimes)
            return;

        var now = DateTime.Now;
        var date = new DateTime(now.Year, now.Month, now.Day, value.Hours, value.Minutes, 0);
        setConfig(date);

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        RequestThemeSwitch();
    }
}
