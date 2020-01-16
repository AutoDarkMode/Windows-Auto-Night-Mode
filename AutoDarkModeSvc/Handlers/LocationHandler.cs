using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;

namespace AutoDarkModeSvc.Handlers
{
    static class LocationHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Refreshes sunrise and sunset based on latittude and longitude found in the configuration file.
        /// </summary>
        /// <param name="configBuilder">config builder for the AutoDarkModeConfig to allow saving</param>
        public static void UpdateSunTime(AutoDarkModeConfigBuilder configBuilder)
        {
            int[] sun = SunDate.CalculateSunriseSunset(configBuilder.config.Location.Lat, configBuilder.config.Location.Lon);
            configBuilder.config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            configBuilder.config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            try
            {
                if (!configBuilder.config.Location.Enabled)
                {
                    configBuilder.config.Location.Enabled = true;
                }
                configBuilder.Save();
                Logger.Info($"Updated sunrise {configBuilder.config.Sunrise.ToString("HH:mm")} and sunset {configBuilder.config.Sunset.ToString("HH:mm")}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "could not update configuration file while updating sundates");
            }
        }

        /// <summary>
        /// Updates the user's geoposition (latitude and longitude) and save to the config
        /// </summary>
        /// <param name="configBuilder">config builder for the AutoDarkModeConfig</param>
        /// <returns></returns>
        public static async Task<bool> UpdateGeoposition(AutoDarkModeConfigBuilder configBuilder)
        {
            var permission = await Geolocator.RequestAccessAsync();
            var success = false;
            switch (permission)
            {
                case GeolocationAccessStatus.Allowed:
                    Geolocator locator = new Geolocator();
                    Geoposition location = await locator.GetGeopositionAsync();
                    BasicGeoposition position = location.Coordinate.Point.Position;
                    configBuilder.config.Location.Lon = position.Longitude;
                    configBuilder.config.Location.Lat = position.Latitude;
                    Logger.Info($"retrieved latitude {position.Latitude} and longitude {position.Longitude}");
                    success = true;
                    break;
                default:
                    configBuilder.config.Location.Enabled = false;
                    Logger.Warn($"no geolocation access, please enable in system settings");
                    break;
            }
            try
            {
                configBuilder.Save();
            }
            catch (Exception e)
            {
                Logger.Error(e, "could not update configuration file while retrieving location");
            }

            return success;
        }

        /// <summary>
        /// Creates a task scheduler entry invoking the thin server for time based switching.
        /// NOT IMPLEMENTED YET!
        /// </summary>
        /// <param name="configBuilder"></param>
        public static void CreateLocationTask(AutoDarkModeConfigBuilder configBuilder)
        {
            UpdateSunTime(configBuilder);
            ApplySunDateOffset(configBuilder.config, out DateTime Sunrise, out DateTime Sunset);
            TaskSchdHandler.CreateSwitchTask(Sunrise.Hour, Sunrise.Minute, Sunset.Hour, Sunset.Minute);
        }

        /// <summary>
        /// Calculate sundates based on a user configurable offset found in AutoDarkModeConfig. 
        /// Call this method to generate the final sunrise and sunset times if location based switching is enabled
        /// </summary>
        /// <param name="config">AutoDarkMoeConfig object</param>
        /// <param name="sunrise_out"></param>
        /// <param name="sunset_out"></param>
        public static void ApplySunDateOffset(AutoDarkModeConfig config, out DateTime sunrise_out, out DateTime sunset_out)
        {
            //Add offset to sunrise and sunset hours using Settings
            DateTime sunrise = config.Sunrise;
            sunrise = sunrise.AddMinutes(config.Location.SunriseOffsetMin);

            DateTime sunset = config.Sunset;
            sunset = sunset.AddMinutes(config.Location.SunsetOffsetMin);

            sunrise_out = sunrise;
            sunset_out = sunset;
        }
    }
}
