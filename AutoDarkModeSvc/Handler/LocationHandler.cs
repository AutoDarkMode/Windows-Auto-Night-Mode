using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using AutoDarkModeSvc;

namespace AutoDarkModeApp
{
    static class LocationHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void UpdateSunTime(AutoDarkModeConfigBuilder configBuilder)
        {
            int[] sun = SunDate.CalculateSunriseSunset(configBuilder.Config.Location.Lat, configBuilder.Config.Location.Lon);
            configBuilder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            configBuilder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            try
            {
                configBuilder.Write();
                Logger.Info($"Updated sunrise {configBuilder.Config.Sunrise.ToString("HH:mm")} and sunset {configBuilder.Config.Sunset.ToString("HH:mm")}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "could not update configuration file while updating sundates");
            }
        }

        private static async Task GetUserPosition(AutoDarkModeConfigBuilder configBuilder)
        {
            Geolocator locator = new Geolocator();
            Geoposition location = await locator.GetGeopositionAsync();
            BasicGeoposition position = location.Coordinate.Point.Position;
            configBuilder.Config.Location.Lon = position.Longitude;
            configBuilder.Config.Location.Lat = position.Latitude;
            try
            {
                configBuilder.Write();
                Logger.Info($"Updated latitude {position.Latitude} and longitude {position.Longitude}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "could not update configuration file while retrieving location");
            }
        }

        public static void CreateLocationTask(AutoDarkModeConfigBuilder configBuilder)
        {
            UpdateSunTime(configBuilder);
            ApplySunDateOffset(configBuilder.Config, out DateTime Sunrise, out DateTime Sunset);
            TaskSchdHandler.CreateSwitchTask(Sunrise.Hour, Sunrise.Minute, Sunset.Hour, Sunset.Minute);
        }

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
