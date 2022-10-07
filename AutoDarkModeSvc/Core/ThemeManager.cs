using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.System.Power;

namespace AutoDarkModeSvc.Core
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ComponentManager cm = ComponentManager.Instance();
        private static readonly GlobalState state = GlobalState.Instance();
        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static void RequestSwitch(SwitchEventArgs e)
        {
            if (state.ForcedTheme == Theme.Dark)
            {
                UpdateTheme(Theme.Dark, e);
                return;
            }
            else if (state.ForcedTheme == Theme.Light)
            {
                UpdateTheme(Theme.Light, e);
                return;
            }

            if (builder.Config.Events.DarkThemeOnBattery)
            {
                if (PowerManager.BatteryStatus == BatteryStatus.Discharging)
                {
                    UpdateTheme(Theme.Dark, e);
                    return;
                }
                if (!builder.Config.AutoThemeSwitchingEnabled)
                {
                    UpdateTheme(Theme.Light, e);
                    return;
                }
            }

            if (e.RequestedTheme.HasValue)
            {
                UpdateTheme(e.RequestedTheme.Value, e);
                return;
            }

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (builder.Config.Governor == Governor.Default)
                {
                    ThemeState ts = new();
                    UpdateTheme(ts.TargetTheme, e, ts.CurrentSwitchTime);
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    if (e.RequestedTheme.HasValue)
                    {
                        if (e.Time.HasValue)
                        {
                            UpdateTheme(e.RequestedTheme.Value, e, e.Time.Value);
                        }
                        else
                        {
                            UpdateTheme(e.RequestedTheme.Value, e);
                        }
                    }
                    else UpdateTheme(state.NightLightActiveTheme, e);
                }
            }
            else
            {
                UpdateTheme(state.LastRequestedTheme, e);
            }
        }

        /// <summary>
        /// Switches the theme and postpones the automatic switch once until the next switching window is reached
        /// If no argument is specified, it will swap the currently active theme
        /// </summary>
        /// <returns>the theme that was switched to</returns>
        public static Theme SwitchThemeAutoPause(Theme target = Theme.Unknown)
        {
            Theme newTheme;
            if (target != Theme.Unknown) newTheme = target;
            else if (state.LastRequestedTheme == Theme.Light) newTheme = Theme.Dark;
            else newTheme = Theme.Light;

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (builder.Config.Governor == Governor.Default)
                {
                    ThemeState ts = new();
                    if (ts.TargetTheme != newTheme)
                    {
                        state.PostponeManager.AddSkipNextSwitch();
                    }
                    else
                    {
                        state.PostponeManager.RemoveSkipNextSwitch();
                    }
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    if (state.NightLightActiveTheme != newTheme)
                        state.PostponeManager.AddSkipNextSwitch();
                    else
                        state.PostponeManager.RemoveSkipNextSwitch();
                }
                RequestSwitch(new(SwitchSource.Manual, newTheme));

            }
            return newTheme;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void UpdateTheme(Theme newTheme, SwitchEventArgs e, DateTime switchTime = new())
        {
            state.LastRequestedTheme = newTheme;

            bool themeModeSwitched = false;
            if (e.Source == SwitchSource.SystemUnlock && builder.Config.WindowsThemeMode.Enabled)
            {
                themeModeSwitched = ThemeHandler.ApplyTheme(builder.Config, newTheme, skipCheck: true);
            }
            else if (builder.Config.WindowsThemeMode.Enabled)
            {
                themeModeSwitched = ThemeHandler.ApplyTheme(builder.Config, newTheme);
            }

            // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
            //cm.UpdateSettings();

            List<ISwitchComponent> componentsToUpdate = cm.GetComponentsToUpdate(newTheme);
            if (componentsToUpdate.Count > 0)
            {
                //logic for our classic mode 2.0, gets the currently active theme for modification
                if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= Helper.MinBuildForNewFeatures)
                {
                    state.ManagedThemeFile.SyncActiveThemeData();
                }

                // if theme mode is disabled, we need to disable energy saver for the modules
                if (!themeModeSwitched) PowerHandler.RequestDisableEnergySaver(builder.Config);
                cm.Run(componentsToUpdate, newTheme, e);
            }


            if (componentsToUpdate.Count > 0 || themeModeSwitched || e.Source == SwitchSource.SystemUnlock)
            {
                // Logic for our classic mode 2.0
                if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= Helper.MinBuildForNewFeatures)
                {
                    try
                    {
                        state.ManagedThemeFile.Save();
                        ThemeHandler.ApplyManagedTheme(builder.Config, Helper.ManagedThemePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "couldn't apply managed theme file: ");
                        return;
                    }
                }

                if (e.Source == SwitchSource.TimeSwitchModule)
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} theme switch performed, source: {Enum.GetName(typeof(SwitchSource), e.Source)}, " +
                        $"{(newTheme == Theme.Light ? "sunrise" : "sunset")}: {switchTime}");
                }
                else if (e.Source == SwitchSource.SystemUnlock)
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} refreshed theme, source: {Enum.GetName(typeof(SwitchSource), e.Source)}");
                }
                else if (e.Source == SwitchSource.NightLightTrackerModule && switchTime.Year > 2000)
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} theme switch performed, source: {Enum.GetName(typeof(SwitchSource), e.Source)}, " +
                    $"{(newTheme == Theme.Light ? "sunrise" : "sunset")}: {switchTime}");
                }
                else
                {
                    Logger.Info($"{Enum.GetName(typeof(Theme), newTheme).ToLower()} theme switch performed, source: {Enum.GetName(typeof(SwitchSource), e.Source)}");
                }
                // disable mitigation after all components and theme switch have been executed
                PowerHandler.RequestRestoreEnergySaver(builder.Config);
            }

        }
    }

    /// <summary>
    /// Contains information about timed theme switching
    /// </summary>
    public class ThemeState
    {
        /// <summary>
        /// Sunrise given by geocoordinates, user input or location service
        /// </summary>
        public DateTime Sunrise { get; private set; }
        /// <summary>
        /// Sunrise given by geocoordinates, user input or location service
        /// </summary>
        public DateTime Sunset { get; private set; }

        private DateTime _adjustedSunrise;
        /// <summary>
        /// Offset-adjusted sunrise given by geocoordinates, user input or location service
        /// </summary>
        public DateTime AdjustedSunrise
        {
            get
            {
                return _adjustedSunrise;
            }
        }
        private DateTime _adjustedSunset;
        /// <summary>
        /// Offset-adjusted sunset given by geocoordinates, user input or location service
        /// </summary>
        public DateTime AdjustedSunset
        {
            get
            {
                return _adjustedSunset;
            }
        }
        /// <summary>
        /// The theme that should be active now
        /// </summary>
        public Theme TargetTheme { get; private set; }
        /// <summary>
        /// Precise Time when the next switch should occur (matches either adjusted sunset or sunrise)
        /// </summary>
        public DateTime NextSwitchTime { get; private set; }

        /// <summary>
        /// Precise Time when the target theme entered its activation window
        /// </summary>
        public DateTime CurrentSwitchTime { get; private set; }

        /// <summary>
        /// Instantiates a new ThemeState class and automatically caluclates timed theme switching data
        /// </summary>
        public ThemeState()
        {
            CalculateThemeState();
        }

        private void CalculateThemeState()
        {
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            Sunrise = builder.Config.Sunrise;
            Sunset = builder.Config.Sunset;
            _adjustedSunrise = Sunrise;
            _adjustedSunset = Sunset;
            if (builder.Config.Location.Enabled)
            {
                LocationHandler.GetSunTimes(builder, out _adjustedSunrise, out _adjustedSunset);
            }
            //the time bewteen sunrise and sunset, aka "day"
            if (Helper.NowIsBetweenTimes(_adjustedSunrise.TimeOfDay, _adjustedSunset.TimeOfDay))
            {
                TargetTheme = Theme.Light;
                CurrentSwitchTime = _adjustedSunrise;
                NextSwitchTime = _adjustedSunset;
            }
            else
            {
                TargetTheme = Theme.Dark;
                CurrentSwitchTime = _adjustedSunset;
                NextSwitchTime = _adjustedSunrise;
            }
        }
    }
}
