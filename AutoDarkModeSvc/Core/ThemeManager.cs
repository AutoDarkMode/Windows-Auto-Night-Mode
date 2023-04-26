#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System.Power;
using static AutoDarkModeLib.IThemeManager2.Flags;

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
                e.OverrideTheme(Theme.Dark, ThemeOverrideSource.ForceFlag);
                UpdateTheme(e);
                return;
            }
            else if (state.ForcedTheme == Theme.Light)
            {
                e.OverrideTheme(Theme.Light, ThemeOverrideSource.ForceFlag);
                UpdateTheme(e);
                return;
            }

            if (builder.Config.Events.DarkThemeOnBattery)
            {
                if (PowerManager.BatteryStatus == BatteryStatus.Discharging)
                {
                    e.OverrideTheme(Theme.Dark, ThemeOverrideSource.BatteryStatus);
                    UpdateTheme(e);
                    return;
                }
                if (!builder.Config.AutoThemeSwitchingEnabled)
                {
                    e.OverrideTheme(Theme.Light, ThemeOverrideSource.BatteryStatus);
                    UpdateTheme(e);
                    return;
                }
            }

            // apply last requested theme if switch is user-postponed
            if (state.PostponeManager.IsUserDelayed || state.PostponeManager.IsSkipNextSwitch)
            {
                e.OverrideTheme(state.RequestedTheme, ThemeOverrideSource.PostponeManager);
                UpdateTheme(e);
                return;
            }

            // non auto switches have priority
            if (e.Theme.HasValue && e.Source != SwitchSource.NightLightTrackerModule)
            {
                UpdateTheme(e);
                return;
            }

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (builder.Config.Governor == Governor.Default)
                {
                    TimedThemeState ts = new();
                    e.OverrideTheme(ts.TargetTheme, ThemeOverrideSource.TimedThemeState);
                    e.UpdateSwitchTime(ts.CurrentSwitchTime);
                    UpdateTheme(e);
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    UpdateTheme(e);
                }
            }
            else
            {
                e.OverrideTheme(state.RequestedTheme, ThemeOverrideSource.Default);
                UpdateTheme(e);
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

            ThemeHandler.EnforceNoMonitorUpdates(builder, state, newTheme);
            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (state.PostponeManager.IsSkipNextSwitch)
                {
                    ToastHandler.InvokePauseOnToggleThemeToast();
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
                        if (!state.PostponeManager.IsUserDelayed)
                        {
                            if (state.PostponeManager.IsSkipNextSwitch) state.PostponeManager.UpdateSkipNextSwitchExpiry();
                            else state.PostponeManager.AddSkipNextSwitch();
                        }
                    }
                    else
                    {
                        state.PostponeManager.RemoveUserClearablePostpones();
                    }
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    if (!state.PostponeManager.IsUserDelayed)
                    {
                        if (state.NightLight.Requested != newTheme)
                            state.PostponeManager.AddSkipNextSwitch();
                        else
                            state.PostponeManager.RemoveUserClearablePostpones();
                    }
                }
            }
            return newTheme;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void UpdateTheme(SwitchEventArgs e)
        {
            if (!e.Theme.HasValue)
            {
                Logger.Info("theme switch requested with no target theme");
            }
            Theme newTheme = e.Theme.Value;
            state.RequestedTheme = newTheme;

            DateTime switchTime = new();
            if (e.SwitchTime.HasValue) switchTime = e.SwitchTime.Value;

            // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
            //cm.UpdateSettings();

            #region determine if theme mode and/or components need to switch
            bool themeModeNeedsUpdate = false;
            if (builder.Config.WindowsThemeMode.Enabled)
            {

                if (e.Source == SwitchSource.SystemUnlock) themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme, skipCheck: true);
                else themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme);
            }

            //todo change to switcheventargs
            (List<ISwitchComponent> componentsToUpdate, bool dwmRefreshRequired) = cm.GetComponentsToUpdate(newTheme);

            // when the app ist launched for the first time, ask for notification
            if (!state.InitSyncSwitchPerformed)
            {
                if ((componentsToUpdate.Count > 0 || themeModeNeedsUpdate) && builder.Config.AutoSwitchNotify.Enabled)
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    state.InitSyncSwitchPerformed = true;
                    // take an educated guess what theme state is most likely to be active at bootup time.
                    // this is necessary such that postpones have the correct requestedtheme!
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
                //todo change to switcheventargs
                cm.RunPreSync(componentsToUpdate, newTheme, e);

                //logic for our classic mode 2.0, gets the currently active theme for modification
                if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
                {
                    // get data from active theme and apply theme fix
                    state.ManagedThemeFile.SyncWithActiveTheme(true);
                }

                //todo change to switcheventargs
                // regular modules that do not need to modify the active theme
                cm.RunPostSync(componentsToUpdate, newTheme, e);

                // if a new theme is being set then dwm updates regardless
                if (dwmRefreshRequired && !themeModeNeedsUpdate)
                {
                    if (builder.Config.WindowsThemeMode.Enabled) ThemeHandler.RefreshDwm(managed: false);
                    else ThemeHandler.RefreshDwm(managed: true);
                }
            }


            // windows theme mode apply theme
            if (themeModeNeedsUpdate)
            {
                ThemeHandler.ApplyTheme(newTheme);
            }


            // non theme mode switches & cleanup
            if (componentsToUpdate.Count > 0 || themeModeNeedsUpdate)
            {
                // Logic for our classic mode 2.0
                if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
                {
                    try
                    {
                        state.ManagedThemeFile.Save();
                        List<ThemeApplyFlags> flagList = null;

                        // if we are using the wallpaper switcher that requires theme files then we cannot ignore the wallpaper settings anymore
                        // This means that this type of wallpaper switch cannot be used for builds older than 22621.1105 because they do not natively support spotlight
                        // Using this with a build that doesn't support spotlight would cause solid color or invalid wallpapers to appear whenever spotlight is enabled.
                        // In addition, a colorization switch always needs a wallpaper refresh
                        bool needsWallpaperRefresh = false;
                        if (componentsToUpdate.Any(c => c is ColorizationSwitch))
                        {
                            if (newTheme == Theme.Light && builder.Config.ColorizationSwitch.Component.LightAutoColorization) needsWallpaperRefresh = true;
                            else if (newTheme == Theme.Dark && builder.Config.ColorizationSwitch.Component.DarkAutoColorization) needsWallpaperRefresh = true;
                        }
                        if (!componentsToUpdate.Any(c => c is WallpaperSwitchThemeFile) && !needsWallpaperRefresh)
                        {
                            flagList = new() { ThemeApplyFlags.IgnoreBackground };
                        }

                        ThemeHandler.ApplyManagedTheme(state.ManagedThemeFile, flagList);
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
                    Logger.Info($"refreshed {Enum.GetName(typeof(Theme), newTheme).ToLower()} theme, source: {Enum.GetName(typeof(SwitchSource), e.Source)}");
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

            if (componentsToUpdate.Count > 0)
            {
                //todo change to switcheventargs
                cm.RunCallbacks(componentsToUpdate, newTheme, e);
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
