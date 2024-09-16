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
using AutoDarkModeSvc.Core;
using Microsoft.Win32;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.System.Power;

namespace AutoDarkModeSvc.Handlers
{
    static class SystemEventHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool darkThemeOnBatteryEnabled;
        private static bool resumeEventEnabled;
        private static DateTime lastSystemTimeChange;
        private static GlobalState state = GlobalState.Instance();
        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static void RegisterThemeEvent()
        {
            if (PowerManager.BatteryStatus == BatteryStatus.NotPresent)
            {
                return;
            }
            if (!darkThemeOnBatteryEnabled)
            {
                Logger.Info("enabling event handler for dark mode on battery state discharging");
                PowerManager.PowerSupplyStatusChanged += PowerManager_BatteryStatusChanged;
                darkThemeOnBatteryEnabled = true;
                PowerManager_BatteryStatusChanged(null, null);
            }
        }

        private static void PowerManager_BatteryStatusChanged(object sender, object e)
        {
            if (PowerManager.PowerSupplyStatus == PowerSupplyStatus.NotPresent)
            {
                Logger.Info("battery discharging, enabling dark mode");
                ThemeManager.UpdateTheme(new(SwitchSource.BatteryStatusChanged, Theme.Dark));
            }
            else
            {
                ThemeManager.RequestSwitch(new(SwitchSource.BatteryStatusChanged));
            }
        }

        public static void DeregisterThemeEvent()
        {
            try
            {
                if (darkThemeOnBatteryEnabled)
                {
                    Logger.Info("disabling event handler for dark mode on battery state discharging");
                    PowerManager.BatteryStatusChanged -= PowerManager_BatteryStatusChanged;
                    darkThemeOnBatteryEnabled = false;
                    ThemeManager.RequestSwitch(new(SwitchSource.BatteryStatusChanged));
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, "while deregistering SystemEvents_PowerModeChanged ");
            }
        }

        public static void RegisterResumeEvent()
        {
            if (!resumeEventEnabled)
            {
                if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
                {
                    Logger.Info("enabling theme refresh at system unlock (win 11)");
                    SystemEvents.SessionSwitch += SystemEvents_Windows11_SessionSwitch;
                }
                else if (builder.Config.Events.Win10AllowLockscreenSwitch)
                {
                    Logger.Info("enabling theme refresh at system resume (win 10)");
                    SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                }
                else
                {
                    Logger.Info("enabling theme refresh at system unlock (win 10)");
                    SystemEvents.SessionSwitch += SystemEvents_Windows11_SessionSwitch;
                }
                // allow postpone timers to sync with system time after resume from sleep
                SystemEvents.PowerModeChanged += SystemEvents_RefreshPostponeTimers;

                resumeEventEnabled = true;
            }
        }

        public static void DeregisterResumeEvent()
        {
            try
            {
                if (resumeEventEnabled)
                {
                    Logger.Info("disabling theme refresh events");
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                    SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                    SystemEvents.SessionSwitch -= SystemEvents_Windows10_SessionSwitch;
                    SystemEvents.SessionSwitch -= SystemEvents_Windows11_SessionSwitch;
                    SystemEvents.PowerModeChanged -= SystemEvents_RefreshPostponeTimers;
                    resumeEventEnabled = false;
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, "while deregistering SystemEvents_PowerModeChanged ");
            }
        }

        private static void SystemEvents_RefreshPostponeTimers(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                try
                {
                    Logger.Debug("resynchronizing postpone timers with system clock after resume");
                    state.PostponeManager.SyncExpiryTimesWithSystemClock();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "error while synchronizing postpone timers with system clock: ");
                }
            }
        }

        private static void SystemEvents_Windows11_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock && !builder.Config.AutoThemeSwitchingEnabled)
            {
                Logger.Info("system unlocked, auto switching disabled, no action");
                state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                return;
            }
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {                
                if (builder.Config.AutoSwitchNotify.Enabled)
                {
                    NotifyAtResume();
                }
                else
                {
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                    if (!state.PostponeManager.IsSkipNextSwitch && !state.PostponeManager.IsUserDelayed)
                    {
                        Logger.Info("system unlocked, refreshing theme");
                        ThemeManager.RequestSwitch(new(SwitchSource.SystemUnlock, refreshDwm: true));
                    }
                    else
                    {
                        Logger.Info($"system unlocked, no refresh due to active user postpones: {state.PostponeManager}");
                    }
                }                
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                state.PostponeManager.Add(new(Helper.PostponeItemSessionLock));
            }
        }

        private static void SystemEvents_Windows10_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock && !builder.Config.AutoThemeSwitchingEnabled)
            {
                Logger.Info("system unlocked, auto switching disabled, no action");
                state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                return;
            }
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                if (builder.Config.AutoSwitchNotify.Enabled)
                {
                    NotifyAtResume();
                }
            }
            else if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (builder.Config.AutoSwitchNotify.Enabled) state.PostponeManager.Add(new(Helper.PostponeItemSessionLock));
            }
        }


        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                if (builder.Config.AutoSwitchNotify.Enabled == false)
                {
                    if (!state.PostponeManager.IsSkipNextSwitch && !state.PostponeManager.IsUserDelayed)
                    {
                        Logger.Info("system resuming from suspended state, refreshing theme");
                        ThemeManager.RequestSwitch(new(SwitchSource.SystemUnlock));
                    }
                    else
                    {
                        Logger.Info($"system resuming from suspended state, no refresh due to active user postpones: {state.PostponeManager}");
                    }
                }
            }
        }


        private static void NotifyAtResume()
        {
            bool shouldNotify = false;
            if (builder.Config.Governor == Governor.NightLight)
            {
                if (state.NightLight.Requested != state.InternalTheme) shouldNotify = true;
            }
            else if (builder.Config.Governor == Governor.Default)
            {
                TimedThemeState ts = new();
                if (ts.TargetTheme != state.InternalTheme) shouldNotify = true;
            }

            if (shouldNotify)
            {
                Logger.Info("system unlocked, prompting user for theme switch");
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(o =>
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
                });
            }
            else
            {
                Logger.Info("system unlocked, theme state valid, not sending notification");
                state.PostponeManager.Remove(new(Helper.PostponeItemSessionLock));
            }
        }

        public static void RegisterTimeChangedEvent()
        {
            SystemEvents.TimeChanged += new EventHandler(TimeChangedEvent);
        }

        public static void DeregisterTimeChangedEvent()
        {
            SystemEvents.TimeChanged -= new EventHandler(TimeChangedEvent);
        }

        private static void TimeChangedEvent(object sender, EventArgs e)
        {
            // Ignore system time events when we're in a session lock state
            // as that will involuntarily trigger system time switch changes for ADM
            if (state.PostponeManager.Get(Helper.PostponeItemSessionLock) != null) return;

            TimeZoneInfo oldTz = TimeZoneInfo.Local;
            DateTime old = DateTime.Now;
            bool oldIsDst = DateTime.Now.IsDaylightSavingTime();
            TimeZoneInfo.ClearCachedData();
            TimeZoneInfo newTz = TimeZoneInfo.Local;
            if (!oldTz.Equals(newTz))
            {
                Logger.Info($"system time zone changed from {oldTz.ToUtcOffsetString()} dst={oldIsDst.ToString().ToLower()} " +
                    $"to {newTz.ToUtcOffsetString()} dst={DateTime.Now.IsDaylightSavingTime().ToString().ToLower()} ");

                // geolocator needs to be updated in case it retrieves data from the windows location service as most likely the geolocation has changed as well
                if (builder.LocationData.DataSourceIsGeolocator != builder.Config.Location.UseGeolocatorService)
                {
                    LocationHandler.UpdateGeoposition(builder).Wait();

                    if (builder.Config.AutoThemeSwitchingEnabled) ThemeManager.RequestSwitch(new(SwitchSource.SystemTimeChanged));
                }
            }
            else
            {
                HandleTimeChangedEvent(old);
            }
        }

        /// <summary>
        /// ensure that themee change requests are only executed once because the Windows event is bugged and fires twice
        /// </summary>
        /// <param name="oldTime">the previous system time</param>
        private static void HandleTimeChangedEvent(DateTime oldTime)
        {
            DateTime nowUtc = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            TimeSpan delta = nowUtc - lastSystemTimeChange;
            lastSystemTimeChange = nowUtc;
            // 1000 ms by default
            if (delta > new TimeSpan(10000 * 1000) || delta < new TimeSpan(10000 * 1000))
            {
                Logger.Info($"system time changed from {oldTime}");
                if (builder.Config.AutoThemeSwitchingEnabled) ThemeManager.RequestSwitch(new(SwitchSource.SystemTimeChanged));
                state.PostponeManager.SyncExpiryTimesWithSystemClock();
            }
        }
    }
}
