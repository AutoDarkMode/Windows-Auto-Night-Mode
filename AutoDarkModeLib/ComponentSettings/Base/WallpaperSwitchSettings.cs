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
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.UI;
using YamlDotNet.Serialization;

namespace AutoDarkModeLib.ComponentSettings.Base
{
    public class WallpaperSwitchSettings
    {
        public WallpaperSwitchSettings()
        {
            Monitors = new();
            SolidColors = new();
            GlobalWallpaper = new();
        }
        public WallpaperType TypeLight { get; set; } = WallpaperType.Individual;
        public WallpaperType TypeDark { get; set; } = WallpaperType.Individual;
        public WallpaperPosition Position { get; set; } = WallpaperPosition.Fill;
        public GlobalWallpaper GlobalWallpaper { get; set; }
        public SolidColors SolidColors { get; set; }
        public List<MonitorSettings> Monitors { get; set; }
    }

    public enum WallpaperType
    {
        Unknown = -1,
        Individual = 0,
        Global = 1,
        All = 2,
        SolidColor = 3,
        Spotlight = 4
    }

    public class GlobalWallpaper
    {
        public string Light { get; set; }
        public string Dark { get; set; }
    }

    public class SolidColors
    {
        public string Light { get; set; } = "#FFFFFF";
        public string Dark { get; set; } = "#000000";
    }

    public class MonitorSettings
    {
        public string Id { get; set; }
        [YamlIgnore]
        public string MonitorString { get; set; }
        [YamlIgnore]
        public bool Connected { get; private set; } = false;
        public string LightThemeWallpaper { get; set; }
        public string DarkThemeWallpaper { get; set; }

        public override string ToString()
        {
            if (MonitorString == null)
            {
                try
                {
                    DisplayMonitor monitor = Task.Run(async () => await GetMonitorInfoAsync()).Result;
                    if (monitor != null)
                    {
                        Connected = true;
                    }
                    if (monitor != null && monitor.DisplayName.Length > 0)
                    {
                        MonitorString = $"{monitor.DisplayName} - {monitor.DisplayAdapterTargetId}";
                    }
                    else
                    {
                        string[] split = Id.Split('#', '&');
                        MonitorString = $"{split[1]} - {split[5][3..]}";
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
