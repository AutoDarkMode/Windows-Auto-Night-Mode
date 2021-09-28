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
        public string Id { get; set; }
        private string MonitorString { get; set; } = null;
        public string LightThemeWallpaper { get; set; }
        public string DarkThemeWallpaper { get; set; }
        public override string ToString()
        {
            if (MonitorString == null)
            {
                try
                {
                    DisplayMonitor monitor = Task.Run(async () => await GetMonitorInfoAsync()).Result;
                    if (monitor != null && monitor.DisplayName.Length > 0)
                    {
                        MonitorString = $"{monitor.DisplayName} - {monitor.DisplayAdapterTargetId}";
                    }
                    else
                    {
                        string[] split = Id.Split('#', '&');
                        MonitorString = $"{split[1]} - {split[5].Substring(3)}";
                    }
                }
                catch
                {
                    MonitorString = Id;
                }
            }
            return MonitorString;
        }
        private async Task<DisplayMonitor> GetMonitorInfoAsync()
        {
            var deviceInfos = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
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
