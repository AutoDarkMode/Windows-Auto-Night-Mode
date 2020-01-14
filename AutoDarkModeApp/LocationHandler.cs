using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;

namespace AutoDarkModeApp
{
    class LocationHandler
    {
        public async Task<int[]> CalculateSunTime(bool background)
        {
            AutoDarkModeConfigBuilder autoDarkModeConfigBuilder = AutoDarkModeConfigBuilder.Instance();
            int[] sundate = new int[4];
            int[] sun = new int[2];
            if (!background)
            {
                BasicGeoposition position = await GetUserPosition();
                Properties.Settings.Default.LocationLatitude = position.Latitude;
                Properties.Settings.Default.LocationLongitude = position.Longitude;
                autoDarkModeConfigBuilder.Config.Location.Lat = position.Latitude;
                autoDarkModeConfigBuilder.Config.Location.Lon = position.Longitude;
                sun = SunDate.CalculateSunriseSunset(position.Latitude, position.Longitude);
            }
            else if (background)
            {
                sun = SunDate.CalculateSunriseSunset(autoDarkModeConfigBuilder.Config.Location.Lat, autoDarkModeConfigBuilder.Config.Location.Lon);
            }


            //Add offset to sunrise and sunset hours using Settings

            //Remove old offset first if new offset is zero to preserve temporal integrity
            DateTime sunrise = new DateTime(1, 1, 1, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            autoDarkModeConfigBuilder.Config.Sunrise = sunrise;
            sunrise = sunrise.AddMinutes(autoDarkModeConfigBuilder.Config.Location.SunriseOffsetMin);


            DateTime sunset = new DateTime(1, 1, 1, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            autoDarkModeConfigBuilder.Config.Sunset = sunset;
            sunset = sunset.AddMinutes(autoDarkModeConfigBuilder.Config.Location.SunsetOffsetMin);

            sundate[0] = sunrise.Hour; //sunrise hour
            sundate[1] = sunrise.Minute; //sunrise minute
            sundate[2] = sunset.Hour; //sunset hour
            sundate[3] = sunset.Minute; //sunset minute
            return sundate;
        }

        private async Task<BasicGeoposition> GetUserPosition()
        {
            var locator = new Geolocator();
            var location = await locator.GetGeopositionAsync();
            var position = location.Coordinate.Point.Position;
            return position;
        }

        public async Task<string> GetCityName()
        {
            BasicGeoposition position = await GetUserPosition();

            Geopoint geopoint = new Geopoint(new BasicGeoposition
            {
                Latitude = position.Latitude,
                Longitude = position.Longitude
            });

            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(geopoint, MapLocationDesiredAccuracy.Low);

            if (result.Status == MapLocationFinderStatus.Success)
            {
                return result.Locations[0].Address.Town;
            }
            else
            {
                return null;
            }
        }

        public async Task SetLocationSilent()
        {
            int[] sundate = await CalculateSunTime(true);
            TaskSchdHandler.CreateSwitchTask(sundate[2], sundate[3], sundate[0], sundate[1]);
        }
    }
}
