using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeConfig;
using System.IO;
using AutoDarkModeSvc.Config;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Display;
using Windows.UI;
using System.Threading;

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
                handler.SetBackgroundColor(ToUint(colors.Dark));
                //Thread.Sleep(500);
                handler.Enable(false);

            }
            else if (newTheme == Theme.Light)
            {
                handler.SetBackgroundColor(ToUint(colors.Light));
                //Thread.Sleep(500);
                handler.Enable(false);
            }
        }

        public static Color GetSolidColor()
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            return ToColor(handler.GetBackgroundColor());
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
            for (uint i = 0; i < handler.GetMonitorDevicePathCount(); i++)
            {
                string monitorId = handler.GetMonitorDevicePathAt(i);
                MonitorSettings monitorSetting = monitorSettings.Find(s => s.Id == monitorId);
                if (monitorSetting != null)
                {
                    if (newTheme == Theme.Dark)
                    {
                        if (!File.Exists(monitorSetting.DarkThemeWallpaper))
                        {
                            Logger.Warn($"target {Enum.GetName(typeof(Theme), newTheme)} wallpaper does not exist (skipping) path ${monitorSetting.DarkThemeWallpaper}, monitor ${monitorId}");
                        }
                        else
                        {
                            handler.SetWallpaper(monitorId, monitorSetting.DarkThemeWallpaper);
                        }
                    }
                    else
                    {
                        if (!File.Exists(monitorSetting.LightThemeWallpaper))
                        {
                            Logger.Warn($"wallpaper does not exist. path ${monitorSetting.DarkThemeWallpaper}, monitor ${monitorId}");
                        }
                        handler.SetWallpaper(monitorId, monitorSetting.LightThemeWallpaper);
                    }
                }
                else
                {
                    Logger.Warn($"no wallpaper config found for monitor {monitorId}, adding missing monitors");
                    DetectMonitors();
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
                wallpapers.Add(new Tuple<string, string>(id, wallpaper));
            }
            return wallpapers;
        }

        public static WallpaperPosition GetPosition()
        {
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            return handler.GetPosition();
        }


        private static async Task<List<DisplayMonitor>> GetMonitorInfosAsync()
        {
            var deviceInfos = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
            List<DisplayMonitor> monitors = new();
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
            var monitors = Task.Run(async () => await GetMonitorInfosAsync()).Result;
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            List<string> monitorIds = new();
            for (uint i = 0; i < handler.GetMonitorDevicePathCount(); i++)
            {
                monitorIds.Add(handler.GetMonitorDevicePathAt(i));
            }
            bool needsUpdate = false;
            foreach (string monitorId in monitorIds)
            {
                if (monitorId.Length == 0)
                {
                    continue;
                }
                MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id == monitorId);
                if (settings == null)
                {
                    Logger.Info($"missing monitor found, adding new default config for: {monitorId}");
                    builder.Config.WallpaperSwitch.Component.Monitors.Add(new MonitorSettings()
                    {
                        DarkThemeWallpaper = handler.GetWallpaper(monitorId),
                        LightThemeWallpaper = handler.GetWallpaper(monitorId),
                        Id = monitorId
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
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
            List<string> monitorIds = new();
            for (uint i = 0; i < handler.GetMonitorDevicePathCount(); i++)
            {
                monitorIds.Add(handler.GetMonitorDevicePathAt(i));
            }
            List<MonitorSettings> connectedSettings = new();
            foreach (string monitorId in monitorIds)
            {
                if (monitorId.Length == 0)
                {
                    continue;
                }
                MonitorSettings settings = builder.Config.WallpaperSwitch.Component.Monitors.Find(m => m.Id == monitorId);
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

            void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
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

            void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

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
                win32.SystemParametersInfo(0x0014, 0, globalWallpaper.Dark, 1 | 2);
                string currentWallpaper = GetGlobalWallpaper();
                return currentWallpaper == globalWallpaper.Dark;

            }
            else if (newTheme == Theme.Light)
            {
                win32.SystemParametersInfo(0x0014, 0, globalWallpaper.Light, 1 | 2);
                string currentWallpaper = GetGlobalWallpaper();
                return currentWallpaper == globalWallpaper.Light;
            }
            return false;
        }

        /// <summary>
        /// Gets the currenvt wallpaper
        /// </summary>
        /// <returns>string with a path to the current wallpapers</returns>
        public static string GetGlobalWallpaper()
        {
            string currentWallpaper = new string('\0', 260);
            win32.SystemParametersInfo(0x0073, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }

        internal sealed class win32
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int SystemParametersInfo(int uAction, int uParam, String lpvParam, int fuWinIni);
        }

        private static uint ToUint(Color c)
        {
            return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
        }

        private static Color ToColor(this uint value)
        {
            return Color.FromArgb((byte)((value >> 24) & 0xFF),
                       (byte)((value >> 16) & 0xFF),
                       (byte)((value >> 8) & 0xFF),
                       (byte)(value & 0xFF));
        }
    }
}
