using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeConfig;
using System.IO;
using Windows.System.Power;
using System.Collections.Generic;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Events;

namespace AutoDarkModeSvc.Core
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ComponentManager cm = ComponentManager.Instance();
        private static readonly GlobalState state = GlobalState.Instance();

        public static void RequestSwitch(AdmConfigBuilder builder, SwitchEventArgs e)
        {
            if (state.ForcedTheme == Theme.Dark)
            {
                UpdateTheme(builder.Config, Theme.Dark, e);
                return;
            }
            else if (state.ForcedTheme == Theme.Light)
            {
                UpdateTheme(builder.Config, Theme.Light, e);
                return;
            }

            if (builder.Config.Events.DarkThemeOnBattery)
            {
                if (PowerManager.BatteryStatus == BatteryStatus.Discharging)
                {
                    UpdateTheme(builder.Config, Theme.Dark, e);
                    return;
                }
                if (!builder.Config.AutoThemeSwitchingEnabled) {
                    UpdateTheme(builder.Config, Theme.Light, e);
                    return;
                }
            }

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                DateTime sunrise = builder.Config.Sunrise;
                DateTime sunset = builder.Config.Sunset;
                if (builder.Config.Location.Enabled)
                {
                    LocationHandler.GetSunTimes(builder, out sunrise, out sunset);
                }
                //the time bewteen sunrise and sunset, aka "day"
                if (Extensions.NowIsBetweenTimes(sunrise.TimeOfDay, sunset.TimeOfDay)) UpdateTheme(builder.Config, Theme.Light, e, sunrise);
                else UpdateTheme(builder.Config, Theme.Dark, e, sunset);
            }
            else
            {
                UpdateTheme(builder.Config, state.LastRequestedTheme, e);
            }
        }

        public static void UpdateTheme(AdmConfig config, Theme newTheme, SwitchEventArgs e, DateTime switchTime = new())
        {
            state.LastRequestedTheme = newTheme;

            bool themeModeSwitched = false;
            if (config.WindowsThemeMode.Enabled)
            {
                themeModeSwitched = ThemeHandler.ApplyTheme(config, newTheme);
            }

            // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
            //cm.UpdateSettings();

            List<ISwitchComponent> componentsToUpdate = cm.GetComponentsToUpdate(newTheme);
            if (componentsToUpdate.Count > 0)
            {
                if (!config.WindowsThemeMode.Enabled)
                {
                    state.ManagedThemeFile.SyncActiveThemeData();
                }
                //if a theme switch did not occur, run mitigations
                if (!themeModeSwitched) PowerHandler.RequestDisableEnergySaver(config);
                cm.Run(componentsToUpdate, newTheme, e);
            }

            // disable mitigation after all components and theme switch have been executed
            if (componentsToUpdate.Count > 0 || themeModeSwitched)
            {
                if (!config.WindowsThemeMode.Enabled)
                {
                    try
                    {
                        state.ManagedThemeFile.Save();
                        ThemeHandler.ApplyManagedTheme(config, Extensions.ManagedThemePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "couldn't apply managed theme file: ");
                    }
                }
                if (e.Source == SwitchSource.TimeSwitchModule)
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} theme switch performed, source: {Enum.GetName(typeof(SwitchSource), e.Source).ToLower()}, " +
                        $"{(newTheme == Theme.Light ? "sunrise" : "sunset")}: {switchTime}");
                }
                else
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} theme switch performed, source: {Enum.GetName(typeof(SwitchSource), e.Source)}");
                }
                PowerHandler.RequestRestoreEnergySaver(config);
            }

        }
    }
}
