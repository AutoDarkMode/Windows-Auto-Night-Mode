#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using System.Runtime.InteropServices;
using AutoDarkModeLib;

namespace AutoDarkModeApp
{
    static class LocationHandler
    {
        public static async Task<string> GetCityName()
        {
            AdmConfigBuilder configBuilder = AdmConfigBuilder.Instance();

            Geopoint geopoint = new(new BasicGeoposition
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
