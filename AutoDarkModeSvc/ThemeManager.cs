/*
 * Originally created by Kuchienkz.
 * Email: wahyu.darkflame@gmail.com
 * 
 * Other Contributors (modified by):
 * Armin2208
 * Spiritreader
*/

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
            PowerHandler.DisableEnergySaver(config);
            if (config.WindowsThemeMode.Enabled)
            {
                ApplyTheme(config, newTheme, automatic, sunset, sunrise);
            }
            RunComponents(newTheme);
            PowerHandler.RestoreEnergySaver(config);


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
    }
}
