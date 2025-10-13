using System.Runtime.InteropServices;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeLib;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace AutoDarkModeApp.Utils.Handlers;

internal static class LocationHandler
{
    private static readonly IErrorService _errorService = App.GetService<IErrorService>();

    public static async Task<string?> GetCityName()
    {
        var configBuilder = AdmConfigBuilder.Instance();

        Geopoint geopoint = new(new BasicGeoposition
        {
            Latitude = configBuilder.LocationData.Lat,
            Longitude = configBuilder.LocationData.Lon
        });

        try
        {
            //TODO: MapLocationFinder will make WinUI app hang on exit, more information on https://github.com/microsoft/microsoft-ui-xaml/issues/10229
            var result = await MapLocationFinder.FindLocationsAtAsync(geopoint, MapLocationDesiredAccuracy.Low);

            if (result.Status == MapLocationFinderStatus.Success)
            {
                return result.Locations[0].Address.Town;
            }
            else
            {
                return null;
            }
        }
        catch (SEHException ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "LocationHandler.GetCityName");
            return string.Format("(~{0,00}, ~{1,00})", geopoint.Position.Latitude, geopoint.Position.Longitude);
        }
    }

    public static void GetSunTimesWithOffset(AdmConfigBuilder builder, out DateTime sunrise_out, out DateTime sunset_out)
    {
        //Add offset to sunrise and sunset hours using Settings
        var sunrise = builder.LocationData.Sunrise;
        sunrise = sunrise.AddMinutes(builder.Config.Location.SunriseOffsetMin);

        var sunset = builder.LocationData.Sunset;
        sunset = sunset.AddMinutes(builder.Config.Location.SunsetOffsetMin);

        sunrise_out = sunrise;
        sunset_out = sunset;
    }
}
