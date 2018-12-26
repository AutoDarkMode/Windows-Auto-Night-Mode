using System;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;
using System.Threading;

namespace AutoThemeChanger
{
    class LocationHandler
    {
        public async Task<int[]> CalculateSunTime()
        {
            int[] sundate = new int[2];
            BasicGeoposition position = await GetUserPosition();
            int[] sun = SunDate.CalculateSunriseSunset(position.Latitude, position.Longitude);
            sundate[0] = new DateTime(1, 1, 1, 1 + sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0).Hour; //sunrise
            sundate[1] = new DateTime(1, 1, 1, 1 + sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0).Hour; //sunset
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

            var source = new CancellationTokenSource(1500);
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(geopoint, MapLocationDesiredAccuracy.Low).AsTask(source.Token);

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

            var accesStatus = await Geolocator.RequestAccessAsync();
            if (accesStatus == GeolocationAccessStatus.Allowed)
            {
                int[] sundate = await CalculateSunTime();
                taskShedHandler.CreateTask(sundate[1], sundate[0]);
            }
        }
    }
}
