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
using System.Runtime.InteropServices;
using System.Linq;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeLib;
using System.IO;
using AutoDarkModeSvc.Monitors;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Display;
using System.Threading;
using System.Drawing;

namespace AutoDarkModeSvc.Handlers
{
    static class WallpaperHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void SetSolidColor(SolidColors colors, Theme newTheme)
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            if (newTheme == Theme.Dark)
            {
                Color dark = HexToColor(colors.Dark);
                int res = handler.SetBackgroundColor(ToUint(dark));
                if (res != 0)
                {
                    Logger.Warn($"set background color exit code was {res}");
                }
                //Thread.Sleep(500);
                handler.Enable(false);
            }
            else if (newTheme == Theme.Light)
            {
                Color light = HexToColor(colors.Light);
                int res = handler.SetBackgroundColor(ToUint(light));
                if (res != 0)
                {
                    Logger.Warn($"set background color exit code was {res}");
                }
                //Thread.Sleep(500);
                handler.Enable(false);
            }
        }

        public static string GetSolidColor()
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            Color c = ToColor(handler.GetBackgroundColor());
            return $"#{(c.ToArgb() & 0x00FFFFFF).ToString("X6")}";

        }

        private static Color HexToColor(string hexString)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r = int.Parse(hexString[..2], System.Globalization.NumberStyles.AllowHexSpecifier);
            int g = int.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            int b = int.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            return Color.FromArgb(0, (byte)r, (byte)g, (byte)b);
        }

        public static string HexToRgb(string hexString)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r = int.Parse(hexString[..2], System.Globalization.NumberStyles.AllowHexSpecifier);
            int g = int.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            int b = int.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

            return $"{r} {g} {b}";
        }

        public static bool SetEnabled(bool state)
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            return handler.Enable(state);
        }

        public static void SetWallpapers(List<MonitorSettings> monitorSettings, WallpaperPosition position, Theme newTheme)
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            handler.SetPosition(position);
            var monitors = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result;

            foreach (var monitor in monitors)
            {
                MonitorSettings monitorSetting = monitorSettings.Find(s => s.Id == monitor.DeviceId);
                if (monitorSetting != null)
                {
                    if (newTheme == Theme.Dark)
                    {
                        if (!File.Exists(monitorSetting.DarkThemeWallpaper))
                        {
                            Logger.Warn($"target {Enum.GetName(typeof(Theme), newTheme)} wallpaper does not exist (skipping) path: {monitorSetting.DarkThemeWallpaper ?? "null"}, monitor ${monitor.DeviceId}");
                        }
                        else
                        {
                            handler.SetWallpaper(monitor.DeviceId, monitorSetting.DarkThemeWallpaper);
                        }
                    }
                    else
                    {
                        if (!File.Exists(monitorSetting.LightThemeWallpaper))
                        {
                            Logger.Warn($"wallpaper does not exist. path ${monitorSetting.DarkThemeWallpaper}, monitor ${monitor.DeviceId}");
                        }
                        handler.SetWallpaper(monitor.DeviceId, monitorSetting.LightThemeWallpaper);
                    }
                }
                else if (monitor.DeviceId == "")
                {
                    Logger.Warn("invalid monitor id, skipping device. This most likely needs a windows restart to be fixed.");
                }
                else
                {
                    Logger.Warn($"no wallpaper config found for monitor {monitor.DeviceId}, adding missing monitors");
                    DisplayHandler.DetectMonitors();
                }
            }
        }

        public static List<Tuple<string, string>> GetWallpapers()
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            List<Tuple<string, string>> wallpapers = new();
            for (uint i = 0; i < handler.GetMonitorDevicePathCount(); i++)
            {
                string id = handler.GetMonitorDevicePathAt(i);
                string wallpaper = handler.GetWallpaper(id);
                if (id.Length > 0)
                {
                    wallpapers.Add(new Tuple<string, string>(id, wallpaper));
                }
            }
            return wallpapers;
        }

        public static WallpaperPosition GetPosition()
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            return handler.GetPosition();
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// This enumeration is used to set and get slideshow options.
        /// </summary>
        public enum DesktopSlideshowOptions
        {
            ShuffleImages = 0x01,     // When set, indicates that the order in which images in the slideshow are displayed can be randomized.
        }


        /// <summary>
        /// This enumeration is used by GetStatus to indicate the current status of the slideshow.
        /// </summary>
        public enum DesktopSlideshowState
        {
            Enabled = 0x01,
            Slideshow = 0x02,
            DisabledByRemoteSession = 0x04,
        }


        /// <summary>
        /// This enumeration is used by the AdvanceSlideshow method to indicate whether to advance the slideshow forward or backward.
        /// </summary>
        public enum DesktopSlideshowDirection
        {
            Forward = 0,
            Backward = 1,
        }

        [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            /// <summary>
            /// Gets the monitor device path.
            /// </summary>
            /// <param name="monitorIndex">Index of the monitor device in the monitor device list.</param>
            /// <returns></returns>
            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetMonitorDevicePathAt(uint monitorIndex);
            /// <summary>
            /// Gets number of monitor device paths.
            /// </summary>
            /// <returns></returns>
            [return: MarshalAs(UnmanagedType.U4)]
            uint GetMonitorDevicePathCount();

            [return: MarshalAs(UnmanagedType.Struct)]
            Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            int SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
            [return: MarshalAs(UnmanagedType.U4)]
            uint GetBackgroundColor();

            void SetPosition([MarshalAs(UnmanagedType.I4)] WallpaperPosition position);
            [return: MarshalAs(UnmanagedType.I4)]
            WallpaperPosition GetPosition();

            void SetSlideshow(IntPtr items);
            IntPtr GetSlideshow();

            void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);
            [PreserveSig]
            uint GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

            uint AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

            DesktopSlideshowDirection GetStatus();

            bool Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
        }

        /// <summary>
        /// CoClass DesktopWallpaper
        /// </summary>
        [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
        public class DesktopWallpaperClass
        {
        }

        /// <summary>
        /// Sets the first background in a wallpaper collection
        /// </summary>
        /// <param name="wallpaperCollection">List with wallpapers</param>
        /// <return>true if wallpaper switch succeeded</return>
        public static bool SetGlobalWallpaper(GlobalWallpaper globalWallpaper, Theme newTheme)
        {
            if (newTheme == Theme.Dark)
            {
                _ = Win32.SystemParametersInfo(0x0014, 0, globalWallpaper.Dark, 1 | 2);
                string currentWallpaper = GetGlobalWallpaper();
                return currentWallpaper == globalWallpaper.Dark;

            }
            else if (newTheme == Theme.Light)
            {
                _ = Win32.SystemParametersInfo(0x0014, 0, globalWallpaper.Light, 1 | 2);
                string currentWallpaper = GetGlobalWallpaper();
                return currentWallpaper == globalWallpaper.Light;
            }
            return false;
        }


        /// <summary>
        /// Advances the slideshow of all monitors by the specified direction.
        /// 
        /// <param name="direction">The direction to advance the slideshow.</param>"
        /// </summary>
        public static void AdvanceSlideshow(DesktopSlideshowDirection direction)
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            for (uint i = 0; i < EnumDisplayDevicesWrapper.ListDisplays().Count; i++)
            {
                handler.AdvanceSlideshow(null, direction);
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Gets the currenvt wallpaper
        /// </summary>
        /// <returns>string with a path to the current wallpapers</returns>
        public static string GetGlobalWallpaper()
        {
            string currentWallpaper = new('\0', 260);
            _ = Win32.SystemParametersInfo(0x0073, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper[..currentWallpaper.IndexOf('\0')];
        }

        internal sealed class Win32
        {
            #pragma warning disable CA2101
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            #pragma warning restore CA2101
            internal static extern int SystemParametersInfo(int uAction, int uParam, String lpvParam, int fuWinIni);
        }

        private static uint ToUint(Color c)
        {
            //return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
            //return (uint)(0xFFFF * c.R + 0xFF * c.G + c.B);
            return (uint)((c.R << 0) | (c.G << 8) | (c.B << 16));
        }

        private static Color ToColor(this uint value)
        {
            return Color.FromArgb((byte)((value >> 24) & 0xFF),
                       (byte)(value & 0xFF),
                       (byte)((value >> 8) & 0xFF),
                       (byte)((value >> 16) & 0xFF));
        }
    }
}
