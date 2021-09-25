using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class WallpaperSwitchSettings
    {
        public WallpaperSwitchSettings()
        {
            Monitors = new();
        }
        private Mode mode;
        //[JsonConverter(typeof(StringEnumConverter))]
        public Mode Mode
        {
            get { return mode; }
            set
            {
                if (value >= 0 && (int)value <= 2)
                {
                    mode = value;
                }
                else
                {
                    // DEFAULT
                    mode = 0;
                }
            }
        }
        public WallpaperPosition Position { get; set; } = WallpaperPosition.Fill;
        public List<MonitorSettings> Monitors { get; set; }
    }

    public class MonitorSettings
    {

        private string id;
        public string Id 
        { 
            get { return id; } 
            set 
            { 
                id = value;
                try
                {
                    DisplayMonitor monitor = Task.Run(async () => await GetMonitorInfoAsync()).Result;
                    if (monitor != null)
                    {
                        MonitorString = $"{monitor.DisplayName} Id: {monitor.DisplayAdapterTargetId}";
                    }
                    else
                    {
                        MonitorString = Id.Substring(Id.IndexOf("#"), Id.IndexOf("&"));
                    }
                }
                catch
                {
                    MonitorString = Id;
                }
            } 
        }
        private string MonitorString { get; set; }
        public string LightThemeWallpaper { get; set; }
        public string DarkThemeWallpaper { get; set; }
        public override string ToString()
        {
            return MonitorString;
        }
        private async Task<DisplayMonitor> GetMonitorInfoAsync()
        {
            var deviceInfos = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
            List<DisplayMonitor> monitors = new();
            foreach (var deviceInfo in deviceInfos)
            {
                if (deviceInfo.Id == Id)
                {
                    var monitor = await DisplayMonitor.FromInterfaceIdAsync(deviceInfo.Id);
                    return monitor;
                }
            }
            return null;
        }
    }

}
