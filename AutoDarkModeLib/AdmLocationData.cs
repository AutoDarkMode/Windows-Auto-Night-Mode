using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeLib
{
    public class AdmLocationData
    {
        public DateTime Sunrise { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 7, 0, 0);
        public DateTime Sunset { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 19, 0, 0);
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool DataSourceIsGeolocator { get; set; }
    }
}
