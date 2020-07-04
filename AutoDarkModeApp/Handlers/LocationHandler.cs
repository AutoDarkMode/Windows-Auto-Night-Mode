using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeApp.Communication;
using AutoDarkMode;
using System.Runtime.InteropServices;

namespace AutoDarkModeApp
{
    class LocationHandler
    {
        public int[] CalculateSunTime()
        {
            AdmConfigBuilder configBuilder = AdmConfigBuilder.Instance();
            int[] sundate = new int[4];

            //Add offset to sunrise and sunset hours using Settings
            DateTime sunrise = configBuilder.LocationData.Sunrise.AddMinutes(configBuilder.Config.Location.SunriseOffsetMin);
            DateTime sunset = configBuilder.LocationData.Sunset.AddMinutes(configBuilder.Config.Location.SunsetOffsetMin);

            sundate[0] = sunrise.Hour; //sunrise hour
            sundate[1] = sunrise.Minute; //sunrise minute
            sundate[2] = sunset.Hour; //sunset hour
            sundate[3] = sunset.Minute; //sunset minute
            return sundate;
        }

        private BasicGeoposition GetUserPosition()
        {
            AdmConfigBuilder configBuilder = AdmConfigBuilder.Instance();
            var position = new BasicGeoposition()
            {
                Latitude = configBuilder.LocationData.Lat,
                Longitude = configBuilder.LocationData.Lon
            };
            return position;
        }

        public async Task<string> GetCityName()
        {
            AdmConfigBuilder configBuilder = AdmConfigBuilder.Instance();

            Geopoint geopoint = new Geopoint(new BasicGeoposition
            {
                Latitude = configBuilder.LocationData.Lat,
                Longitude = configBuilder.LocationData.Lon
            });

            try
            {
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
            catch (SEHException)
            {
                return string.Format("(~{0,00}, ~{1,00})", geopoint.Position.Latitude, geopoint.Position.Longitude);
            }
        }
    }
}
