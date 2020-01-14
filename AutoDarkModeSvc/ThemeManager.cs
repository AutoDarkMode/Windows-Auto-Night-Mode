using System;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
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
            if (!config.Location.Disabled)
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
                && rtc.CurrentColorPrevalence == config.AccentColorTaskbar)
            {
                return;
            }

            if (config.AppsTheme == (int)Mode.Switch)
            {
                RegistryHandler.SetAppsTheme((int)newTheme);
                rtc.CurrentAppsTheme = newTheme;
            }
            if (config.SystemTheme == (int)Mode.Switch)
            {
                RegistryHandler.SetSystemTheme((int)newTheme);
                rtc.CurrentSystemTheme = newTheme;
            }
            if (!config.Wallpaper.Disabled)
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

            if (config.AccentColorTaskbar && config.SystemTheme == (int)Mode.Switch)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(200);
                    RegistryHandler.SetColorPrevalence((int)newTheme);
                });                
            }
        }
    }
}
