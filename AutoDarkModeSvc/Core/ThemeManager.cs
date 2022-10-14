using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System.Power;

namespace AutoDarkModeSvc.Core
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ComponentManager cm = ComponentManager.Instance();
        private static readonly GlobalState state = GlobalState.Instance();
        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static void RequestSwitch(SwitchEventArgs switchArgs)
        {
            if (state.ForcedTheme == Theme.Dark)
            {
                UpdateTheme(Theme.Dark, switchArgs);
                return;
            }
            else if (state.ForcedTheme == Theme.Light)
            {
                UpdateTheme(Theme.Light, switchArgs);
                return;
            }

            if (builder.Config.Events.DarkThemeOnBattery)
            {
                if (PowerManager.BatteryStatus == BatteryStatus.Discharging)
                {
                    UpdateTheme(Theme.Dark, switchArgs);
                    return;
                }
                if (!builder.Config.AutoThemeSwitchingEnabled)
                {
                    UpdateTheme(Theme.Light, switchArgs);
                    return;
                }
            }

            // apply last requested theme if switch is postponed
            if (state.PostponeManager.IsPostponed)
            {
                UpdateTheme(state.RequestedTheme, switchArgs);
                return;
            }

            if (switchArgs.Theme.HasValue && switchArgs.Source != SwitchSource.NightLightTrackerModule)
            {
                UpdateTheme(switchArgs.Theme.Value, switchArgs);
                return;
            }

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (builder.Config.Governor == Governor.Default)
                {
                    TimedThemeState ts = new();
                    UpdateTheme(ts.TargetTheme, switchArgs, ts.CurrentSwitchTime);
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    if (switchArgs.Theme.HasValue)
                    {
                        if (switchArgs.Time.HasValue)
                        {
                            UpdateTheme(switchArgs.Theme.Value, switchArgs, switchArgs.Time.Value);
                        }
                        else
                        {
                            UpdateTheme(switchArgs.Theme.Value, switchArgs);
                        }
                    }
                    else UpdateTheme(state.NightLight.Current, switchArgs);
                }
            }
            else
            {
                UpdateTheme(state.RequestedTheme, switchArgs);
            }
        }

        /// <summary>
        /// Switches the theme and postpones the automatic switch once until the next switching window is reached
        /// If no argument is specified, it will swap the currently active theme
        /// </summary>
        /// <returns>the theme that was switched to</returns>
        public static Theme SwitchThemeAutoPause(Theme target = Theme.Unknown, SwitchSource source = SwitchSource.Manual)
        {
            Theme newTheme = PrepareSwitchAutoPause(target);
            RequestSwitch(new(source, newTheme));
            return newTheme;
        }

        public static Theme SwitchThemeAutoPauseAndNotify()
        {
            Theme newTheme = PrepareSwitchAutoPause();

            ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (state.PostponeManager.IsSkipNextSwitch)
                {
                    ToastHandler.InvokeTogglePauseNotificationToast();
                    Task.Run(async () => await Task.Delay(TimeSpan.FromSeconds(2))).Wait();
                }
                RequestSwitch(new(SwitchSource.Manual, newTheme));
            }
            else
            {
                RequestSwitch(new(SwitchSource.Manual, newTheme));
            }
            return newTheme;
        }

        private static Theme PrepareSwitchAutoPause(Theme target = Theme.Unknown)
        {
            Theme newTheme;
            if (target != Theme.Unknown) newTheme = target;
            else if (state.RequestedTheme == Theme.Light) newTheme = Theme.Dark;
            else newTheme = Theme.Light;

            // pre-set requested theme to set skip times correctly
            state.RequestedTheme = newTheme;

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (builder.Config.Governor == Governor.Default)
                {
                    // set the timer pause theme early to allow the postpone manager to update its pause times correctly
                    TimedThemeState ts = new();
                    if (ts.TargetTheme != newTheme)
                    {
                        if (state.PostponeManager.IsSkipNextSwitch) state.PostponeManager.UpdateSkipNextSwitchExpiry();
                        else state.PostponeManager.AddSkipNextSwitch();
                    }
                    else
                    {
                        state.PostponeManager.RemoveUserClearablePostpones();
                    }
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    if (state.NightLight.Current != newTheme)
                        state.PostponeManager.AddSkipNextSwitch();
                    else
                        state.PostponeManager.RemoveUserClearablePostpones();
                }
            }
            return newTheme;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void UpdateTheme(Theme newTheme, SwitchEventArgs e, DateTime switchTime = new())
        {
            state.RequestedTheme = newTheme;

            // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
            //cm.UpdateSettings();

            #region determine if theme mode and/or components need to switch
            bool themeModeNeedsUpdate = false;
            if (builder.Config.WindowsThemeMode.Enabled)
            {
                if (e.Source == SwitchSource.SystemUnlock) themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme, skipCheck: true);
                else themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme);
            }

            List<ISwitchComponent> componentsToUpdate = cm.GetComponentsToUpdate(newTheme);


            // when the app ist launched for the first time, ask for notification
            if (!state.InitSyncSwitchPerformed)
            {
                if ((componentsToUpdate.Count > 0 || themeModeNeedsUpdate) && builder.Config.AutoSwitchNotify.Enabled)
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    state.InitSyncSwitchPerformed = true;
                    // take an educated guess what theme state is most likely to be active at bootup time.
                    try
                    {
                        state.RequestedTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "couldn't initialize apps theme state");
                    }
                    return;
                }
            }

            #endregion

            #region apply themes and run components
            if (componentsToUpdate.Count > 0)
            {
                // if theme mode is disabled, we need to disable energy saver for the modules
                PowerHandler.RequestDisableEnergySaver(builder.Config);

                // run modules that require their data to be re-synced with auto dark mode after running because they modify the active theme file
                cm.RunPreSync(componentsToUpdate, newTheme, e);

                //logic for our classic mode 2.0, gets the currently active theme for modification
                if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= Helper.MinBuildForNewFeatures)
                {
                    state.ManagedThemeFile.SyncActiveThemeData();
                }

                // regular modules that do not require theme file synchronization
                cm.RunPostSync(componentsToUpdate, newTheme, e);
            }
            // windows theme mode apply theme
            if (themeModeNeedsUpdate) ThemeHandler.ApplyTheme(newTheme);


            // non theme mode switches & cleanup
            if (componentsToUpdate.Count > 0 || themeModeNeedsUpdate || e.Source == SwitchSource.SystemUnlock)
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
            #endregion

            if (!state.InitSyncSwitchPerformed)
            {
                state.InitSyncSwitchPerformed = true;
            }
        }
    }

    /// <summary>
    /// Contains information about timed theme switching
    /// </summary>
    public class TimedThemeState
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
        /// Precise Time when the target theme entered its activation window <br/> 
        /// (when the last switch occurred or should have occurred)
        /// </summary>
        public DateTime CurrentSwitchTime { get; private set; }

        /// <summary>
        /// Instantiates a new ThemeState class and automatically caluclates timed theme switching data
        /// </summary>
        public TimedThemeState()
        {
            Calculate();
        }

        private void Calculate()
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
