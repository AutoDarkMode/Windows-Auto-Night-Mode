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
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Monitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using static AutoDarkModeSvc.Handlers.WallpaperHandler;

namespace AutoDarkModeSvc.Handlers
{
    internal static class DisplayHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static async Task<List<DisplayMonitor>> GetMonitorInfosAsync()
        {
            var deviceInfos = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
            List<DisplayMonitor> monitors = [];
            foreach (var deviceInfo in deviceInfos)
            {
                DisplayMonitor monitor = await DisplayMonitor.FromInterfaceIdAsync(deviceInfo.Id);
                monitors.Add(monitor);
            }
            return monitors;
        }

        /// <summary>
        /// Adds missing monitors to the configuration file
        /// If a monitor configuration is not found,
        /// it will automatically create a configuration with the respective monitor's current wallpaper
        /// </summary>
        public static void DetectMonitors()
        {
            var monitors = Task.Run(GetMonitorInfosAsync).Result;
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            List<string> monitorIds = new();
            bool needsUpdate = false;
            foreach (var monitor in monitors)
            {
                MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id == monitor.DeviceId);
                if (settings == null)
                {
                    Logger.Info($"missing monitor found, adding new default config for: {monitor.DeviceId}");
                    builder.Config.WallpaperSwitch.Component.Monitors.Add(new MonitorSettings()
                    {
                        DarkThemeWallpaper = handler.GetWallpaper(monitor.DeviceId),
                        LightThemeWallpaper = handler.GetWallpaper(monitor.DeviceId),
                        Id = monitor.DeviceId
                    });
                    needsUpdate = true;
                }
            }
            if (needsUpdate)
            {
                GlobalState state = GlobalState.Instance();
                state.SkipConfigFileReload = true;
                builder.Save();
            }
        }

        public static void CleanUpMonitors()
        {
            var monitors = Task.Run(GetMonitorInfosAsync).Result;
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            List<MonitorSettings> connectedSettings = new();
            foreach (var monitor in monitors)
            {
                MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id == monitor.DeviceId);
                if (settings != null)
                {
                    connectedSettings.Add(settings);
                }
            }
            int diff = builder.Config.WallpaperSwitch.Component.Monitors.Count - connectedSettings.Count;
            if (diff != 0)
            {
                Logger.Info($"removing {diff} disconnected monitors");
                GlobalState state = GlobalState.Instance();
                state.SkipConfigFileReload = true;
                builder.Config.WallpaperSwitch.Component.Monitors = connectedSettings;
                builder.Save();
            }
        }

        public static List<(string, string)> EnumerateMonitors()
        {
            List<(string, string)> enumeratedDevices = new();
            List<DisplayInfo> displayInfos = EnumDisplayDevicesWrapper.ListDisplays();
            builder.Config.WallpaperSwitch.Component.Monitors.ForEach(m =>
            {
                foreach (var display in displayInfos)
                {
                    if (m.Id.Contains(display.DisplayName)) {
                        enumeratedDevices.Add((m.Id, display.DisplayNumber));
                    }
                }
            });
            return enumeratedDevices;
        }
    }


    public class DisplayInfo
    {
        public string DeviceName { get; set; }
        public string DisplayName { get; set; }
        public string DisplayNumber { get; set; }
        public string DeviceString { get; set; }
        public string DeviceID { get; set; }
        public string DeviceKey { get; set; }
    }

    internal static class EnumDisplayDevicesWrapper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public static List<DisplayInfo> ListDisplays()
        {
            List<DisplayInfo> displayInfos = new();
            DISPLAY_DEVICE d = new();
            d.cb = Marshal.SizeOf(d);
            try
            {
                for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
                {
                    if (d.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop))
                    {
                        d.cb = Marshal.SizeOf(d);
                        EnumDisplayDevices(d.DeviceName, 0, ref d, 0);

                        string displayNumber = d.DeviceName.Substring(d.DeviceName.IndexOf("DISPLAY") + 7);
                        displayNumber = displayNumber.Substring(0, displayNumber.IndexOf("\\")).Replace("\\", "");

                        string displayName = d.DeviceID.Substring(d.DeviceID.IndexOf("\\") + 1, d.DeviceID.Length - d.DeviceID.IndexOf("\\") - 1);
                        displayName = displayName.Substring(0, displayName.IndexOf("\\")).Replace("\\", "");

                        displayInfos.Add(new DisplayInfo
                        {
                            DeviceName = d.DeviceName,
                            DeviceString = d.DeviceString,
                            DeviceID = d.DeviceID,
                            DeviceKey = d.DeviceKey,
                            DisplayName = displayName,
                            DisplayNumber = displayNumber
                        });
                    }
                    d.cb = Marshal.SizeOf(d);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not retrieve display adapter info using EnumDisplayDevices:");
            }
            return displayInfos;
        }
    }
}
