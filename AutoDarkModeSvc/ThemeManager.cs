using System;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using AutoDarkModeApp;

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

        public static void SwitchTheme(AutoDarkModeConfig config, Theme newTheme)
        {
            RuntimeConfig rtc = RuntimeConfig.Instance();

            if (rtc.CurrentAppsTheme == newTheme 
                && rtc.CurrentSystemTheme == newTheme 
                && rtc.CurrentColorPrevalence == config.AccentColorTaskbarEnabled
                && rtc.CurrentWallpaperTheme == newTheme)
            {
                return;
            }

            if (config.AppsTheme == (int)Mode.DarkOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Dark);
                rtc.CurrentAppsTheme = Theme.Dark;
            }
            else if (config.AppsTheme == (int)Mode.LightOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Light);
                rtc.CurrentAppsTheme = Theme.Light;
            }
            else
            { 
                RegistryHandler.SetAppsTheme((int)newTheme);
                rtc.CurrentAppsTheme = newTheme;
            }

            if (config.SystemTheme == (int)Mode.DarkOnly)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
                rtc.CurrentSystemTheme = Theme.Dark;
            }
            else if (config.SystemTheme == (int)Mode.LightOnly)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Light);
                rtc.CurrentSystemTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetSystemTheme((int)newTheme);
                rtc.CurrentSystemTheme = newTheme;
            }

            if(config.EdgeTheme == (int)Mode.DarkOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Light);
            }
            else if (config.EdgeTheme == (int)Mode.LightOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Dark);
            }
            else
            {
                RegistryHandler.SetEdgeTheme((int)newTheme);
            }

            if (config.Wallpaper.Enabled)
            {
                if (newTheme == Theme.Dark || rtc.CurrentWallpaperTheme == Theme.Undefined)
                {
                    WallpaperHandler.SetBackground(config.Wallpaper.DarkThemeWallpapers);
                    rtc.CurrentWallpaperTheme = newTheme;
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
                    RegistryHandler.SetColorPrevalence((int)newTheme);
                });                
            }
            Logger.Info($"switched to {newTheme} theme as current theme differs from requested theme");
        }
    }
}
