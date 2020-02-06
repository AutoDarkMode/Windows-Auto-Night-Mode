using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;

namespace AutoDarkModeSvc
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void TimedSwitch(AutoDarkModeConfig config)
        {
            DateTime sunrise = config.Sunrise;
            DateTime sunset = config.Sunset;
            if (config.Location.Enabled)
            {
                LocationHandler.ApplySunDateOffset(config, out sunrise, out sunset);
            }
            //the time bewteen sunrise and sunset, aka "day"
            if (Extensions.NowIsBetweenTimes(sunrise.TimeOfDay, sunset.TimeOfDay))
            {
                SwitchTheme(config, Theme.Light);
            }
            else
            {
                SwitchTheme(config, Theme.Dark);
            }
        }

        /// <summary>
        /// checks if any theme needs an update because the runtimeconfig differs from the actual configured state
        /// </summary>
        /// <param name="config">AutoDarkModeConfig instance</param>
        /// <param name="newTheme">new theme that is requested</param>
        /// <returns></returns>
        private static bool NeedsUpdate(AutoDarkModeConfig config, Theme newTheme)
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();

            if (config.Wallpaper.Enabled)
            {
                if (rtc.CurrentWallpaperTheme == Theme.Undefined || rtc.CurrentWallpaperTheme != newTheme)
                {
                    return true;
                }
            }

            if (    (config.SystemTheme == Mode.DarkOnly  && rtc.CurrentSystemTheme != Theme.Dark )
                 || (config.SystemTheme == Mode.LightOnly && rtc.CurrentSystemTheme != Theme.Light)
                 || (config.SystemTheme == Mode.Switch    && rtc.CurrentSystemTheme != newTheme   )
               )
            {
                return true;
            }

            if (    (config.AppsTheme == Mode.DarkOnly  && rtc.CurrentAppsTheme != Theme.Dark )
                 || (config.AppsTheme == Mode.LightOnly && rtc.CurrentAppsTheme != Theme.Light)
                 || (config.AppsTheme == Mode.Switch    && rtc.CurrentAppsTheme != newTheme   )
               )
            {
                return true;
            }

            if (   (config.EdgeTheme == Mode.DarkOnly  && rtc.CurrentEdgeTheme != Theme.Dark )
                || (config.EdgeTheme == Mode.LightOnly && rtc.CurrentEdgeTheme != Theme.Light)
                || (config.EdgeTheme == Mode.Switch    && rtc.CurrentEdgeTheme != newTheme   )
              )
            {
                return true;
            }

            return false;
        }

        public static void SwitchTheme(AutoDarkModeConfig config, Theme newTheme)
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();

            if (!NeedsUpdate(config, newTheme))
            {
                return;
            }

            var oldsys = rtc.CurrentSystemTheme;
            var oldapp = rtc.CurrentAppsTheme;
            var oldedg = rtc.CurrentEdgeTheme;
            var oldwal = rtc.CurrentWallpaperTheme;

            if (config.AppsTheme == Mode.DarkOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Dark);
                rtc.CurrentAppsTheme = Theme.Dark;
            }
            else if (config.AppsTheme == Mode.LightOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Light);
                rtc.CurrentAppsTheme = Theme.Light;
            }
            else
            { 
                RegistryHandler.SetAppsTheme((int)newTheme);
                rtc.CurrentAppsTheme = newTheme;
            }

            if (config.SystemTheme == Mode.DarkOnly)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
                rtc.CurrentSystemTheme = Theme.Dark;
            }
            else if (config.SystemTheme == Mode.LightOnly)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Light);
                rtc.CurrentSystemTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetSystemTheme((int)newTheme);
                rtc.CurrentSystemTheme = newTheme;
            }

            if (config.EdgeTheme == Mode.DarkOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Dark);
            }
            else if (config.EdgeTheme == Mode.LightOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Light);
            }
            else
            {
                RegistryHandler.SetEdgeTheme((int)newTheme);
            }

            if (config.Wallpaper.Enabled)
            {
                if (newTheme == Theme.Dark)
                {
                    var success = WallpaperHandler.SetBackground(config.Wallpaper.DarkThemeWallpapers);
                    if (success)
                    {
                        rtc.CurrentWallpaperTheme = newTheme;
                    }
                }
                else
                {
                    WallpaperHandler.SetBackground(config.Wallpaper.LightThemeWallpapers);
                    rtc.CurrentWallpaperTheme = newTheme;
                }
            }

            if (config.AccentColorTaskbarEnabled)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(200);
                    if (rtc.CurrentSystemTheme == Theme.Dark)
                    {
                        RegistryHandler.SetColorPrevalence(1);
                    }
                    else
                    {
                        RegistryHandler.SetColorPrevalence(0);
                    }
                });                
            }
            Logger.Info($"theme switch performed");
            Logger.Info($"theme: {newTheme} with modes (s:{config.SystemTheme}, a:{config.AppsTheme}, e:{config.EdgeTheme}, w:{config.Wallpaper.Enabled})");
            Logger.Info($"was (s:{oldsys}, a:{oldapp}, e:{oldedg}, w:{oldwal}),");
            Logger.Info($"is (s:{rtc.CurrentSystemTheme}, a:{rtc.CurrentAppsTheme}, e:{rtc.CurrentEdgeTheme}, w:{rtc.CurrentWallpaperTheme})");
        }
    }
}
