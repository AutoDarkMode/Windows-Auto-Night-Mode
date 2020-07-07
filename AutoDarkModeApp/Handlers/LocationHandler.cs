using System;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using System.Runtime.InteropServices;
using AutoDarkModeSvc.Config;

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

        /// <summary>
        /// Calculate sundates based on a user configurable offset found in AutoDarkModeConfig. 
        /// Call this method to generate the final sunrise and sunset times if location based switching is enabled
        /// </summary>
        /// <param name="config">AutoDarkMoeConfig object</param>
        /// <param name="sunrise_out"></param>
        /// <param name="sunset_out"></param>
        public static void GetSunTimesWithOffset(AdmConfigBuilder builder, out DateTime sunrise_out, out DateTime sunset_out)
            {
                //Add offset to sunrise and sunset hours using Settings
                DateTime sunrise = builder.LocationData.Sunrise;
                sunrise = sunrise.AddMinutes(builder.Config.Location.SunriseOffsetMin);

                DateTime sunset = builder.LocationData.Sunset;
                sunset = sunset.AddMinutes(builder.Config.Location.SunsetOffsetMin);

                sunrise_out = sunrise;
                sunset_out = sunset;
            }
        }
}
