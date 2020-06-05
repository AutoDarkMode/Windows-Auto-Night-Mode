using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using System.Threading;
using System.IO;

namespace AutoDarkModeSvc
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void TimedSwitch(AutoDarkModeConfig config)
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();
            if (rtc.ForcedTheme == Theme.Dark) 
            { 
                if (config.ClassicMode)
                {
                    ApplyThemeOptions(config, Theme.Dark);
                }
                else
                {
                    ApplyTheme(config, Theme.Dark);
                }

                return;
            } 
            else if (rtc.ForcedTheme == Theme.Light) 
            {
                if (config.ClassicMode)
                {
                    ApplyThemeOptions(config, Theme.Light);
                }
                else
                {
                    ApplyTheme(config, Theme.Light);
                }
                return;
            }

            DateTime sunrise = config.Sunrise;
            DateTime sunset = config.Sunset;
            if (config.Location.Enabled)
            {
                LocationHandler.ApplySunDateOffset(config, out sunrise, out sunset);
            }
            //the time bewteen sunrise and sunset, aka "day"
            if (Extensions.NowIsBetweenTimes(sunrise.TimeOfDay, sunset.TimeOfDay))
            {
                if (config.ClassicMode)
                {
                    ApplyThemeOptions(config, Theme.Light, true, sunset, sunrise);
                }
                else
                {
                    ApplyTheme(config, Theme.Light, true, sunset, sunrise);
                }
            }
            else
            {
                if (config.ClassicMode)
                {
                    ApplyThemeOptions(config, Theme.Dark, true, sunset, sunrise);
                }
                else
                {
                    ApplyTheme(config, Theme.Dark, true, sunset, sunrise);
                }
            }
        }

        public static void ApplyTheme(AutoDarkModeConfig config, Theme newTheme, bool automatic = false, DateTime sunset = new DateTime(), DateTime sunrise = new DateTime())
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();
            if (config.DarkThemePath == null || config.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return;
            }
            if (!File.Exists(config.DarkThemePath)) {
                Logger.Error($"invalid dark theme path: {config.DarkThemePath}");
                return;
            }
            if (!File.Exists(config.LightThemePath))
            {
                Logger.Error($"invalid light theme path : {config.LightThemePath}");
                return;
            }
            if (!config.DarkThemePath.EndsWith(".theme") || !config.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return;
            }

            if (Path.GetFileNameWithoutExtension(config.DarkThemePath) != rtc.CurrentWindowsThemeName && newTheme == Theme.Dark)
            {
                if (automatic)
                {
                    Logger.Info($"automatic dark theme switch pending, sunset: {sunset.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info("switching to dark theme");
                }
                SetOfficeTheme(config.Office.Mode, newTheme, rtc, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);
                ThemeHandler.Apply(config.DarkThemePath);
            }
            else if (Path.GetFileNameWithoutExtension(config.LightThemePath) != rtc.CurrentWindowsThemeName && newTheme == Theme.Light)
            {
                if (automatic)
                {
                    Logger.Info($"automatic light theme switch pending, sunrise: {sunrise.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info("switching to light theme");
                }
                SetOfficeTheme(config.Office.Mode, newTheme, rtc, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);
                ThemeHandler.Apply(config.LightThemePath);
            }
        }

        public static void ApplyThemeOptions(AutoDarkModeConfig config, Theme newTheme, bool automatic = false, DateTime sunset = new DateTime(), DateTime sunrise = new DateTime())
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();

            if (!ThemeOptionsNeedUpdate(config, newTheme))
            {
                return;
            }

            var oldsys = rtc.CurrentSystemTheme;
            var oldapp = rtc.CurrentAppsTheme;
            var oldedg = rtc.CurrentEdgeTheme;
            var oldwal = rtc.CurrentWallpaperTheme;
            var oldoff = rtc.CurrentOfficeTheme;

            SetAppsTheme(config.AppsTheme, newTheme, rtc);
            SetEdgeTheme(config.EdgeTheme, newTheme, rtc);

            SetWallpaper(newTheme, rtc, config.Wallpaper.DarkThemeWallpapers, config.Wallpaper.LightThemeWallpapers, config.Wallpaper.Enabled);
            SetOfficeTheme(config.Office.Mode, newTheme, rtc, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);

            //run async to delay at specific parts due to color prevalence not switching icons correctly
            int taskdelay = config.Tunable.AccentColorSwitchDelay;
            Task.Run(async () =>
            {
                await SetSystemTheme(config.SystemTheme, newTheme, taskdelay, rtc, config);

                if (automatic)
                {
                    Logger.Info($"theme switch invoked automatically. Sunrise:{sunrise.ToString("HH:mm:ss")}, Sunset{sunrise.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info($"theme switch invoked manually");
                }
                Logger.Info($"theme: {newTheme} with modes (s:{config.SystemTheme}, a:{config.AppsTheme}, e:{config.EdgeTheme}, w:{config.Wallpaper.Enabled}, o:{config.Office.Enabled}");
                Logger.Info($"was (s:{oldsys}, a:{oldapp}, e:{oldedg}, w:{oldwal} o:{oldoff}");
                Logger.Info($"is (s:{rtc.CurrentSystemTheme}, a:{rtc.CurrentAppsTheme}, e:{rtc.CurrentEdgeTheme}, w:{rtc.CurrentWallpaperTheme}, o:{rtc.CurrentOfficeTheme}");
            });
        }

        private static void SetAppsTheme(Mode mode, Theme newTheme, RuntimeConfig rtc)
        {
            if (mode == Mode.DarkOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Dark);
                rtc.CurrentAppsTheme = Theme.Dark;
            }
            else if (mode == Mode.LightOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Light);
                rtc.CurrentAppsTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetAppsTheme((int)newTheme);
                rtc.CurrentAppsTheme = newTheme;
            }
        }

        private async static Task SetSystemTheme(Mode mode, Theme newTheme, int taskdelay, RuntimeConfig rtc, AutoDarkModeConfig config)
        {
            // Set system theme
            if (mode == Mode.DarkOnly)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
                rtc.CurrentSystemTheme = Theme.Dark;
                await Task.Delay(taskdelay);
                if (config.AccentColorTaskbarEnabled)
                {
                    RegistryHandler.SetColorPrevalence(1);
                }
                else
                {
                    RegistryHandler.SetColorPrevalence(0);
                }
            }
            else if (config.SystemTheme == Mode.LightOnly)
            {
                RegistryHandler.SetColorPrevalence(0);
                await Task.Delay(taskdelay);
                RegistryHandler.SetSystemTheme((int)Theme.Light);
                rtc.CurrentSystemTheme = Theme.Light;
            }
            else
            {
                if (config.AccentColorTaskbarEnabled)
                {
                    if (newTheme == Theme.Light)
                    {
                        RegistryHandler.SetColorPrevalence(0);
                        await Task.Delay(taskdelay);
                    }
                }
                RegistryHandler.SetSystemTheme((int)newTheme);
                if (config.AccentColorTaskbarEnabled)
                {
                    if (newTheme == Theme.Dark)
                    {
                        await Task.Delay(taskdelay);
                        RegistryHandler.SetColorPrevalence(1);
                    }
                }
                rtc.CurrentSystemTheme = newTheme;
            }
        }

        private static void SetEdgeTheme(Mode mode, Theme newTheme, RuntimeConfig rtc)
        {
            if (mode == Mode.DarkOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Dark);
                rtc.CurrentEdgeTheme = Theme.Dark;
            }
            else if (mode == Mode.LightOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Light);
                rtc.CurrentEdgeTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetEdgeTheme((int)newTheme);
                rtc.CurrentEdgeTheme = newTheme;
            }
        }

        private static void SetOfficeTheme(Mode mode, Theme newTheme, RuntimeConfig rtc, byte lightTheme, byte darkTheme, bool enabled)
        {
            if (enabled)
            {
                if (mode == Mode.DarkOnly)
                {
                    RegistryHandler.OfficeTheme(darkTheme);
                    rtc.CurrentOfficeTheme = Theme.Dark;
                }
                else if (mode == Mode.LightOnly)
                {
                    RegistryHandler.OfficeTheme(lightTheme);
                    rtc.CurrentOfficeTheme = Theme.Light;
                }
                else
                {
                    if (newTheme == Theme.Dark)
                    {
                        RegistryHandler.OfficeTheme(darkTheme);
                    }
                    else
                    {
                        RegistryHandler.OfficeTheme(lightTheme);
                    }
                    rtc.CurrentOfficeTheme = newTheme;
                }
            }
        }

        private static void SetWallpaper(Theme newTheme, RuntimeConfig rtc, System.Collections.Generic.ICollection<string> darkThemeWallpapers,
            System.Collections.Generic.ICollection<string> lightThemeWallpapers, bool enabled)
        {
            if (enabled)
            {
                if (newTheme == Theme.Dark)
                {
                    var success = WallpaperHandler.SetBackground(darkThemeWallpapers);
                    if (success)
                    {
                        rtc.CurrentWallpaperTheme = newTheme;
                    }
                }
                else
                {
                    WallpaperHandler.SetBackground(lightThemeWallpapers);
                    rtc.CurrentWallpaperTheme = newTheme;
                }
            }
        }

        /// <summary>
        /// checks if any theme needs an update because the runtimeconfig differs from the actual configured state
        /// </summary>
        /// <param name="config">AutoDarkModeConfig instance</param>
        /// <param name="newTheme">new theme that is requested</param>
        /// <returns></returns>
        private static bool ThemeOptionsNeedUpdate(AutoDarkModeConfig config, Theme newTheme)
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();
            if (config.Wallpaper.Enabled)
            {
                if (rtc.CurrentWallpaperTheme == Theme.Undefined || rtc.CurrentWallpaperTheme != newTheme)
                {
                    return true;
                }
            }

            if (ComponentNeedsUpdate(config.SystemTheme, rtc.CurrentSystemTheme, newTheme))
            {
                return true;
            }

            if (ComponentNeedsUpdate(config.AppsTheme, rtc.CurrentAppsTheme, newTheme))
            {
                return true;
            }

            if (ComponentNeedsUpdate(config.EdgeTheme, rtc.CurrentEdgeTheme, newTheme))
            {
                return true;
            }

            if (ComponentNeedsUpdate(config.Office.Mode, rtc.CurrentOfficeTheme, newTheme))
            {
                return true;
            }

            return false;
        }

        private static bool ComponentNeedsUpdate(Mode componentMode, Theme currentComponentTheme, Theme newTheme)
        {
            if ((componentMode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
                || (componentMode == Mode.LightOnly && currentComponentTheme != Theme.Light)
                || (componentMode == Mode.Switch && currentComponentTheme != newTheme)
              )
            {
                return true;
            }
            return false;
        }
    }
}
