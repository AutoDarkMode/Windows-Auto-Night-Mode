using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using AutoDarkModeConfig;
using System.IO;
using Windows.System.Power;
using System.Collections.Generic;

namespace AutoDarkModeSvc
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static void RunComponents(Theme newTheme)
        {
            ComponentManager cm = ComponentManager.Instance();
            cm.UpdateSettings();
            cm.Run(newTheme);
        }

        public static void TimedSwitch(AdmConfigBuilder builder)
        {

            GlobalState state = GlobalState.Instance();
            if (state.ForcedTheme == Theme.Dark)
            {
                SwitchTheme(builder.Config, Theme.Dark);
                return;
            }
            else if (state.ForcedTheme == Theme.Light)
            {
                SwitchTheme(builder.Config, Theme.Light);
                return;
            }

            DateTime sunrise = builder.Config.Sunrise;
            DateTime sunset = builder.Config.Sunset;
            if (builder.Config.Location.Enabled)
            {
                LocationHandler.GetSunTimesWithOffset(builder, out sunrise, out sunset);
            }
            //the time bewteen sunrise and sunset, aka "day"
            if (Extensions.NowIsBetweenTimes(sunrise.TimeOfDay, sunset.TimeOfDay))
            {
                // ensure that the theme doesn't switch to light mode if the battery is discharging
                if (builder.Config.Events.DarkThemeOnBattery && PowerManager.BatteryStatus != BatteryStatus.Discharging)
                {
                    SwitchTheme(builder.Config, Theme.Light, true, sunset, sunrise);
                }
                else if (!builder.Config.Events.DarkThemeOnBattery)
                {
                    SwitchTheme(builder.Config, Theme.Light, true, sunset, sunrise);
                }
            }
            else
            {
                SwitchTheme(builder.Config, Theme.Dark, true, sunset, sunrise);
            }
        }

        public static void SwitchTheme(AdmConfig config, Theme newTheme, bool automatic = false, DateTime sunset = new DateTime(), DateTime sunrise = new DateTime())
        {
            if (!automatic)
            {
                Logger.Info($"theme switch invoked manually");
            }
            if (config.WindowsThemeMode.Enabled)
            {
                ApplyTheme(config, newTheme, automatic, sunset, sunrise);
            }
            else
            {
                PowerHandler.DisableEnergySaver(config);
                ApplyThemeOptions(config, newTheme, automatic, sunset, sunrise);
            }
            RunComponents(newTheme);
            if (!config.WindowsThemeMode.Enabled)
            {
                PowerHandler.RestoreEnergySaver(config);
            }

        }
        /// <summary>
        /// Applies the theme using the KAWAII Theme switcher logic for windows theme files
        /// </summary>
        /// <param name="config"></param>
        /// <param name="newTheme"></param>
        /// <param name="automatic"></param>
        /// <param name="sunset"></param>
        /// <param name="sunrise"></param>
        private static void ApplyTheme(AdmConfig config, Theme newTheme, bool automatic, DateTime sunset, DateTime sunrise)
        {
            GlobalState state = GlobalState.Instance();
            if (config.WindowsThemeMode.DarkThemePath == null || config.WindowsThemeMode.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return;
            }
            if (!File.Exists(config.WindowsThemeMode.DarkThemePath))
            {
                Logger.Error($"invalid dark theme path: {config.WindowsThemeMode.DarkThemePath}");
                return;
            }
            if (!File.Exists(config.WindowsThemeMode.LightThemePath))
            {
                Logger.Error($"invalid light theme path : {config.WindowsThemeMode.LightThemePath}");
                return;
            }
            if (!config.WindowsThemeMode.DarkThemePath.EndsWith(".theme") || !config.WindowsThemeMode.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return;
            }

            if (Path.GetFileNameWithoutExtension(config.WindowsThemeMode.DarkThemePath) != state.CurrentWindowsThemeName && newTheme == Theme.Dark)
            {
                ThemeHandler.Apply(config.WindowsThemeMode.DarkThemePath);
            }
            else if (Path.GetFileNameWithoutExtension(config.WindowsThemeMode.LightThemePath) != state.CurrentWindowsThemeName && newTheme == Theme.Light)
            {
                ThemeHandler.Apply(config.WindowsThemeMode.LightThemePath);
            }
        }

        private static void ApplyThemeOptions(AdmConfig config, Theme newTheme, bool automatic, DateTime sunset, DateTime sunrise)
        {
            GlobalState state = GlobalState.Instance();

            if (!ThemeOptionsNeedUpdate(config, newTheme))
            {
                return;
            }
            PowerHandler.DisableEnergySaver(config);
            var oldwal = state.CurrentWallpaperTheme;

            SetWallpaper(newTheme, state, config.Wallpaper.DarkThemeWallpapers, config.Wallpaper.LightThemeWallpapers, config.Wallpaper.Enabled);
            //run async to delay at specific parts due to color prevalence not switching icons correctly
            PowerHandler.RestoreEnergySaver(config);
            Logger.Info($"was (w:{oldwal}");
            Logger.Info($"is (w:{state.CurrentWallpaperTheme})");
        }


        private static void SetWallpaper(Theme newTheme, GlobalState rtc, List<string> darkThemeWallpapers,
            List<string> lightThemeWallpapers, bool enabled)
        {
            if (enabled)
            {
                try
                {
                    if (newTheme == Theme.Dark)
                    {
                        var success = WallpaperHandler.SetBackground(darkThemeWallpapers);
                        if (success)
                        {
                            rtc.CurrentWallpaperTheme = newTheme;
                            rtc.CurrentWallpaperPath = WallpaperHandler.GetBackground();
                        }
                    }
                    else
                    {
                        var success = WallpaperHandler.SetBackground(lightThemeWallpapers);
                        if (success)
                        {
                            rtc.CurrentWallpaperTheme = newTheme;
                            rtc.CurrentWallpaperPath = WallpaperHandler.GetBackground();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not set wallpaper");
                }
            }
        }

        /// <summary>
        /// checks if any theme needs an update because the runtimeconfig differs from the actual configured state
        /// </summary>
        /// <param name="config">AutoDarkModeConfig instance</param>
        /// <param name="newTheme">new theme that is requested</param>
        /// <returns></returns>
        private static bool ThemeOptionsNeedUpdate(AdmConfig config, Theme newTheme)
        {
            GlobalState state = GlobalState.Instance();
            if (config.Wallpaper.Enabled)
            {
                if (state.CurrentWallpaperTheme == Theme.Undefined || state.CurrentWallpaperTheme != newTheme)
                {
                    return true;
                }
            }

            if (WallpaperNeedsUpdate(config.Wallpaper.Enabled, state.CurrentWallpaperPath, config.Wallpaper.LightThemeWallpapers,
                config.Wallpaper.DarkThemeWallpapers, state.CurrentWallpaperTheme, newTheme))
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

        private static bool WallpaperNeedsUpdate(bool enabled, string currentWallpaperPath, List<string> lightThemeWallpapers,
            List<string> darkThemeWallpapers, Theme currentComponentTheme, Theme newTheme)
        {
            if (enabled)
            {
                if (currentComponentTheme != newTheme)
                {
                    return true;
                }
                else if (newTheme == Theme.Dark && !darkThemeWallpapers.Contains(currentWallpaperPath))
                {
                    return true;
                }
                else if (newTheme == Theme.Light && !lightThemeWallpapers.Contains(currentWallpaperPath))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
