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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using Windows.System.Power;
using static AutoDarkModeLib.IThemeManager2.Flags;
using static AutoDarkModeSvc.Handlers.WallpaperHandler;

namespace AutoDarkModeSvc.Core;

internal static class ThemeManager
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly ComponentManager cm = ComponentManager.Instance();
    private static readonly GlobalState state = GlobalState.Instance();
    private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

    public static void RequestSwitch(SwitchEventArgs e)
    {
        // process force switches
        switch (state.ForcedTheme)
        {
            case Theme.Dark:
                e.OverrideTheme(Theme.Dark, ThemeOverrideSource.ForceFlag);
                UpdateTheme(e);
                return;
            case Theme.Light:
                e.OverrideTheme(Theme.Light, ThemeOverrideSource.ForceFlag);
                UpdateTheme(e);
                return;
        }

        // apply last requested theme if switch is postponed
        if (state.PostponeManager.IsPostponed)
        {
            e.OverrideTheme(state.InternalTheme, ThemeOverrideSource.PostponeManager);
            UpdateTheme(e);
            return;
        }

        // battery switch if the initial event was missed
        if (builder.Config.Events.DarkThemeOnBattery)
        {
            if (PowerManager.PowerSupplyStatus == PowerSupplyStatus.NotPresent)
            {
                // guard against auto switch triggering this event
                if (e.Source == SwitchSource.TimeSwitchModule) return;
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

        // process switches with a requested theme set before automatic ones
        if (e.Theme != Theme.Automatic)
        {
            UpdateTheme(e);
            return;
        }

        // recalculate timed theme state on every call
        if (builder.Config.AutoThemeSwitchingEnabled)
        {
            switch (builder.Config.Governor)
            {
                case Governor.Default:
                    {
                        TimedThemeState ts = new();
                        e.OverrideTheme(ts.TargetTheme, ThemeOverrideSource.TimedThemeState);
                        e.UpdateSwitchTime(ts.CurrentSwitchTime);
                        UpdateTheme(e);
                        break;
                    }
                case Governor.NightLight:
                    e.OverrideTheme(state.NightLight.Requested, ThemeOverrideSource.NightLight);
                    UpdateTheme(e);
                    break;
            }
        }
        else
        {
            e.OverrideTheme(state.InternalTheme, ThemeOverrideSource.Default);
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
        else if (state.InternalTheme == Theme.Light) newTheme = Theme.Dark;
        else newTheme = Theme.Light;

        // pre-set requested theme to set skip times correctly
        state.InternalTheme = newTheme;

        if (builder.Config.AutoThemeSwitchingEnabled)
        {
            switch (builder.Config.Governor)
            {
                case Governor.Default:
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
                            state.PostponeManager.RemoveSkipNextSwitch();
                        }

                        break;
                    }

                case Governor.NightLight:
                    if (!state.PostponeManager.IsUserDelayed)
                    {
                        if (state.NightLight.Requested != newTheme)
                            state.PostponeManager.AddSkipNextSwitch();
                        else
                            state.PostponeManager.RemoveSkipNextSwitch();
                    }
                    break;
            }
        }
        return newTheme;
    }


    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void UpdateTheme(SwitchEventArgs e)
    {
        if (e.Theme == Theme.Unknown || e.Theme == Theme.Automatic)
        {
            Logger.Info("Theme switch requested with no target theme");
            return;
        }
        Theme newTheme = e.Theme;
        state.InternalTheme = newTheme;

        DateTime switchTime = new();
        if (e.SwitchTime.HasValue) switchTime = e.SwitchTime.Value;

        // this is possibly necessary in the future if the config is internally updated and switchtheme is called before it is saved
        //cm.UpdateSettings();

        #region determine if theme mode and/or components need to switch
        bool themeModeNeedsUpdate = false;
        if (builder.Config.WindowsThemeMode.Enabled)
        {

            switch (e.Source)
            {
                case SwitchSource.SystemUnlock:
                case SwitchSource.SystemResume:
                    // check for theme changes on system unlock or resume
                    GlobalState.Instance().RefreshThemes(AdmConfigBuilder.Instance().Config);
                    themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme);
                    break;
                default:
                    themeModeNeedsUpdate = ThemeHandler.ThemeModeNeedsUpdate(newTheme);
                    break;
            }
        }

        (List<ISwitchComponent> componentsToUpdate, DwmRefreshType neededDwmRefresh, DwmRefreshType providedDwmRefresh) = cm.GetComponentsToUpdate(e);
        if (e.RefreshDwm) neededDwmRefresh = DwmRefreshType.Full;

        #endregion

        #region logic for adm startup
        // when the app ist launched for the first time, ask for notification
        if (!state.InitSyncSwitchPerformed && (componentsToUpdate.Count > 0 || themeModeNeedsUpdate) && builder.Config.AutoSwitchNotify.Enabled)
        {
            ToastHandler.InvokeDelayAutoSwitchNotifyToast();
            state.InitSyncSwitchPerformed = true;
            // take an educated guess what theme state is most likely to be active at bootup time.
            // this is necessary such that postpones have the correct requestedtheme!
            try
            {
                if (builder.PostponeData.InternalThemeAtExit == Theme.Unknown)
                    state.InternalTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Couldn't initialize apps theme state");
            }
            return;
        }
        #endregion

        bool themeSwitched = false;

        #region apply themes and run components
        if (e.RefreshDwm || componentsToUpdate.Count > 0 || themeModeNeedsUpdate)
        {
            PowerHandler.RequestDisableEnergySaver(builder.Config);

            // run modules that require their data to be re-synced with auto dark mode after running because they modify the active theme file
            cm.RunPreSync(componentsToUpdate, e);

            //logic for our classic mode 2.0, gets the currently active theme for modification
            if (builder.Config.WindowsThemeMode.Enabled == false && Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                // get data from active theme and apply theme fix
                state.ManagedThemeFile.SyncWithActiveTheme(true);
            }

            // regular modules that do not need to modify the active theme
            cm.RunPostSync(componentsToUpdate, e);

            #region dwm refresh
            // force refresh should only happen if there are actually operations that switch parts of windows that require dwm refreshes
            if (builder.Config.Tunable.AlwaysFullDwmRefresh &&
               (providedDwmRefresh != DwmRefreshType.Full && neededDwmRefresh != DwmRefreshType.None || themeModeNeedsUpdate))
            {
                Logger.Info("dwm management: full refresh requested by user");
                if (builder.Config.WindowsThemeMode.Enabled)
                {
                    ThemeHandler.RefreshDwmFull(managed: false, e);
                    themeModeNeedsUpdate = true;
                }
                else ThemeHandler.RefreshDwmFull(managed: true, e);
            }
            // on managed mode if the dwm refresh is insufficient, queue a full refresh
            else if (builder.Config.WindowsThemeMode.Enabled == false && (providedDwmRefresh < neededDwmRefresh))
            {
                Logger.Info($"dwm management: provided refresh type {Enum.GetName(providedDwmRefresh).ToLower()} insufficent, minimum: {Enum.GetName(neededDwmRefresh).ToLower()}");
                ThemeHandler.RefreshDwmFull(managed: true, e);
            }
            // on managed mode, if the dwm refresh is provided by the components, no refresh is required
            else if ((providedDwmRefresh >= neededDwmRefresh) && (neededDwmRefresh != DwmRefreshType.None))
            {
                Logger.Info($"dwm management: requested {Enum.GetName(providedDwmRefresh).ToLower()} refresh will be performed by component(s) in queue");
            }
            // if windows theme mode is enabled the user needs to ensure that the selected themes refresh properly
            else if (themeModeNeedsUpdate)
            {
                Logger.Info($"dwm management: refresh is expected to be handled by user");
            }
            else
            {
                Logger.Info("dwm management: no refresh required");
            }
            #endregion

            // Logic for managed mode
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
                        switch (newTheme)
                        {
                            case Theme.Light when builder.Config.ColorizationSwitch.Component.LightAutoColorization:
                                needsWallpaperRefresh = true;
                                break;
                            case Theme.Dark when builder.Config.ColorizationSwitch.Component.DarkAutoColorization:
                                needsWallpaperRefresh = true;
                                break;
                        }
                    }
                    if (!componentsToUpdate.Any(c => c is WallpaperSwitchThemeFile) && !needsWallpaperRefresh)
                    {
                        flagList = [ThemeApplyFlags.IgnoreBackground];
                    }

                    ThemeHandler.ApplyManagedTheme(state.ManagedThemeFile, flagList);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't apply managed theme file: ");
                    return;
                }
            }

            // Windows theme mode apply theme
            if (themeModeNeedsUpdate)
            {
                PowerHandler.RequestDisableEnergySaver(builder.Config);
                ThemeHandler.ApplyUnmanagedTheme(newTheme);
                themeSwitched = true; // should use return value from ApplyUnmanagedTheme?
            }

            //TODO: change to switch eventargs
            cm.RunCallbacks(componentsToUpdate, newTheme, e);

            bool shuffleCondition = false;
            if (!builder.Config.WindowsThemeMode.Enabled)
            {
                shuffleCondition = state.ManagedThemeFile.Slideshow.Enabled && (state.ManagedThemeFile.Slideshow.Shuffle == 1);
            }
            if (shuffleCondition)
            {
                Logger.Debug("Advancing slideshow in shuffled mode");
                AdvanceSlideshow(DesktopSlideshowDirection.Forward);

                /*
                // randomize slideshow forwarding when shuffle is enabled
                Logger.Debug("Slideshow and shuffling enabled, rolling the dice...");
                Random rng = new();
                // 80% chance to advance slideshow
                if (rng.Next(0, 100) < 80)
                {
                    Logger.Debug("Dice rolled, advancing slideshow...");
                }
                */
            }

            themeSwitched = true;

            PowerHandler.RequestRestoreEnergySaver(builder.Config);

        }
        #endregion

        if (themeSwitched)
        {
            switch (e.Source)
            {
                case SwitchSource.TimeSwitchModule:
                    Logger.Info($"{Enum.GetName(newTheme).ToLower()} theme switch performed, source: {Enum.GetName(e.Source)}, " +
                                    $"{(newTheme == Theme.Light ? "sunrise" : "sunset")}: {switchTime}");
                    break;
                case SwitchSource.SystemUnlock:
                    Logger.Info($"refreshed {Enum.GetName(newTheme).ToLower()} theme, source: {Enum.GetName(e.Source)}");
                    break;
                case SwitchSource.NightLightTrackerModule when switchTime.Year > 2000:
                    Logger.Info($"{Enum.GetName(newTheme).ToLower()} theme switch performed, source: {Enum.GetName(e.Source)}, " +
                        $"{(newTheme == Theme.Light ? "sunrise" : "sunset")}: {switchTime}");
                    break;
                default:
                    Logger.Info($"{Enum.GetName(newTheme).ToLower()} theme switch performed, source: {Enum.GetName(e.Source)}");
                    break;
            }
        }

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
    public DateTime AdjustedSunrise => _adjustedSunrise;
    private DateTime _adjustedSunset;
    /// <summary>
    /// Offset-adjusted sunset given by geocoordinates, user input or location service
    /// </summary>
    public DateTime AdjustedSunset => _adjustedSunset;
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
