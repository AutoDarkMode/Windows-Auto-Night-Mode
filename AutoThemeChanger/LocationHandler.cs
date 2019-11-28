using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;

namespace AutoThemeChanger
{
    class LocationHandler
    {
        public async Task<int[]> CalculateSunTime(bool background)
        {
            int[] sundate = new int[4];
            int[] sun = new int[2];
            if (!background)
            {
                BasicGeoposition position = await GetUserPosition();
                Properties.Settings.Default.LocationLatitude = position.Latitude;
                Properties.Settings.Default.LocationLongitude = position.Longitude;
                sun = SunDate.CalculateSunriseSunset(position.Latitude, position.Longitude);
            }
            else if (background)
            {
                sun = SunDate.CalculateSunriseSunset(Properties.Settings.Default.LocationLatitude, Properties.Settings.Default.LocationLongitude);
            }
            sundate[0] = new DateTime(1, 1, 1, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0).Hour; //sunrise hour
            sundate[1] = new DateTime(1, 1, 1, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0).Minute; //sunrise minute
            sundate[2] = new DateTime(1, 1, 1, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0).Hour; //sunset hour
            sundate[3] = new DateTime(1, 1, 1, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0).Minute; //sunset minute
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
            TaskShedHandler taskShedHandler = new TaskShedHandler();
            int[] sundate = await CalculateSunTime(true);
            taskShedHandler.CreateTask(sundate[2], sundate[3], sundate[0], sundate[1]);
        }
    }
}
