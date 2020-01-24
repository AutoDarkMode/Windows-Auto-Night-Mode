using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeApp.Communication;
using AutoDarkMode;

namespace AutoDarkModeApp
{
    class LocationHandler
    {
        public int[] CalculateSunTime(bool background)
        {
            AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
            int[] sundate = new int[4];
            int[] sun = new int[2];
            if (!background)
            {
                BasicGeoposition position = GetUserPosition();
                Properties.Settings.Default.LocationLatitude = position.Latitude;
                Properties.Settings.Default.LocationLongitude = position.Longitude;
                configBuilder.Config.Location.Lat = position.Latitude;
                configBuilder.Config.Location.Lon = position.Longitude;
                sun = SunDate.CalculateSunriseSunset(position.Latitude, position.Longitude);
            }
            else if (background)
            {
                sun = SunDate.CalculateSunriseSunset(configBuilder.Config.Location.Lat, configBuilder.Config.Location.Lon);
            }


            //Add offset to sunrise and sunset hours using Settings

            //Remove old offset first if new offset is zero to preserve temporal integrity
            DateTime sunrise = new DateTime(1, 1, 1, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            configBuilder.Config.Sunrise = sunrise;
            sunrise = sunrise.AddMinutes(configBuilder.Config.Location.SunriseOffsetMin);


            DateTime sunset = new DateTime(1, 1, 1, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            configBuilder.Config.Sunset = sunset;
            sunset = sunset.AddMinutes(configBuilder.Config.Location.SunsetOffsetMin);

            sundate[0] = sunrise.Hour; //sunrise hour
            sundate[1] = sunrise.Minute; //sunrise minute
            sundate[2] = sunset.Hour; //sunset hour
            sundate[3] = sunset.Minute; //sunset minute
            return sundate;
        }

        private BasicGeoposition GetUserPosition()
        {
            AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
            var position = new BasicGeoposition()
            {
                Latitude = configBuilder.Config.Location.Lat,
                Longitude = configBuilder.Config.Location.Lon
            };
            return position;
        }

        public async Task<string> GetCityName()
        {
            AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();

            Geopoint geopoint = new Geopoint(new BasicGeoposition
            {
                Latitude = configBuilder.Config.Location.Lat,
                Longitude = configBuilder.Config.Location.Lon
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
    }
}
