using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Time and Location handling
public partial class AutoSwitchViewModel : ObservableRecipient
{
    [RelayCommand]
    private void SaveCoordinates()
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        SafeApplyTheme();
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        SafeApplyTheme();
    }
}
