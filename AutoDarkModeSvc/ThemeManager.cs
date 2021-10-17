using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using AutoDarkModeConfig;
using System.IO;
using Windows.System.Power;
using System.Collections.Generic;
using AutoDarkModeSvc.Interfaces;

namespace AutoDarkModeSvc
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ComponentManager cm = ComponentManager.Instance();

        public static void TimedSwitch(AdmConfigBuilder builder, bool automatic = true)
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
                    SwitchTheme(builder.Config, Theme.Light, automatic, sunset, sunrise);
                }
                else if (!builder.Config.Events.DarkThemeOnBattery)
                {
                    SwitchTheme(builder.Config, Theme.Light, automatic, sunset, sunrise);
                }
            }
            else
            {
                SwitchTheme(builder.Config, Theme.Dark, automatic, sunset, sunrise);
            }
        }

        public static void SwitchTheme(AdmConfig config, Theme newTheme, bool automatic = false, DateTime sunset = new DateTime(), DateTime sunrise = new DateTime())
        {
            if (!automatic)
            {
                Logger.Info($"theme switch invoked manually");
            }

            bool themeModeSwitched = false;
            if (config.WindowsThemeMode.Enabled)
            {
                themeModeSwitched = ApplyTheme(config, newTheme);
            }

            // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
            //cm.UpdateSettings();

            List<ISwitchComponent> componentsToUpdate = cm.GetComponentsToUpdate(newTheme);
            if (componentsToUpdate.Count > 0)
            {
                //if a theme switch did not occur, run mitigations
                if (!themeModeSwitched)
                {
                    PowerHandler.DisableEnergySaver(config);
                }
                cm.Run(componentsToUpdate, newTheme);
            }

            // disable mitigation after all components and theme switch have been executed
            if (componentsToUpdate.Count > 0 || themeModeSwitched)
            {
                if (newTheme == Theme.Light && automatic)
                {
                    Logger.Info($"automatic light theme switch performed, sunrise: {sunrise:HH:mm:ss}");
                }
                else if (newTheme == Theme.Dark && automatic)
                {
                    Logger.Info($"automatic dark theme switch performed, sunset: {sunset:HH:mm:ss}");
                }
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
        /// <returns>true if an update was performed; false otherwise</returns>
        private static bool ApplyTheme(AdmConfig config, Theme newTheme)
        {
            GlobalState state = GlobalState.Instance();
            if (config.WindowsThemeMode.DarkThemePath == null || config.WindowsThemeMode.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return false;
            }
            if (!File.Exists(config.WindowsThemeMode.DarkThemePath))
            {
                Logger.Error($"invalid dark theme path: {config.WindowsThemeMode.DarkThemePath}");
                return false;
            }
            if (!File.Exists(config.WindowsThemeMode.LightThemePath))
            {
                Logger.Error($"invalid light theme path: {config.WindowsThemeMode.LightThemePath}");
                return false;
            }
            if (!config.WindowsThemeMode.DarkThemePath.EndsWith(".theme") || !config.WindowsThemeMode.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return false;
            }

            // TODO change tracking when having active theme monitor disabled
            if (newTheme == Theme.Dark && state.CurrentWindowsThemeName.Equals(Path.GetFileNameWithoutExtension(config.WindowsThemeMode.LightThemePath), StringComparison.Ordinal))
            {
                PowerHandler.DisableEnergySaver(config);
                ThemeHandler.Apply(config.WindowsThemeMode.DarkThemePath);
                return true;
            }
            else if (newTheme == Theme.Light && state.CurrentWindowsThemeName.Equals(Path.GetFileNameWithoutExtension(config.WindowsThemeMode.DarkThemePath), StringComparison.Ordinal))
            {
                PowerHandler.DisableEnergySaver(config);
                ThemeHandler.Apply(config.WindowsThemeMode.LightThemePath);
                return true;
            }
            return false;
        }
    }
}
