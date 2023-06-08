using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace AutoDarkModeSvc.Governors
{
    public class NightLightGovernor : IAutoDarkModeGovernor
    {
        public Governor Type => Governor.NightLight;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private DateTime lastNightLightQueryTime = DateTime.Now;
        private Theme nightLightState = Theme.Unknown;
        private ManagementEventWatcher nightLightKeyWatcher;
        private GlobalState state = GlobalState.Instance();
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool init = true;
        private bool queuePostponeRemove = false;
        private IAutoDarkModeModule Master { get; }

        public NightLightGovernor(IAutoDarkModeModule master)
        {
            Master = master;
        }


        public GovernorEventArgs Run()
        {
            DateTime adjustedTime;

            //apply offsets to the latest available switch times
            if (nightLightState == Theme.Dark)
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunsetOffsetMin);
            }
            else
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunriseOffsetMin);
            }

            // if the switch time is in the future, we need to set the global night light theme to the opposite of the internally tracked oned
            // Otherwise the incorrect theme will show up
            if (DateTime.Compare(adjustedTime, DateTime.Now) > 0 && !init)
            {
                if (nightLightState == Theme.Light) state.NightLight.Requested = Theme.Dark;
                else if (nightLightState == Theme.Dark) state.NightLight.Requested = Theme.Light;
            }
            else if (state.NightLight.Requested != nightLightState)
            {
                state.NightLight.Requested = nightLightState;
            }

            // if auto switch notify is enabled and we are approaching the switch window, we need to show a notification
            if (builder.Config.AutoSwitchNotify.Enabled && !init && state.PostponeManager.Get(Helper.PostponeItemSessionLock) == null)
            {
                if (!state.PostponeManager.IsGracePeriod && Helper.NowIsBetweenTimes(adjustedTime.AddMilliseconds(-TimerFrequency.Main).TimeOfDay, adjustedTime.AddMilliseconds(TimerFrequency.Main).TimeOfDay)
                    && state.NightLight.Requested != state.InternalTheme)
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    return new(true);
                }
            }

            bool reportSwitchWindow = !init;

            if (!Helper.NowIsBetweenTimes(adjustedTime.AddMilliseconds(-TimerFrequency.Main).TimeOfDay, adjustedTime.AddMilliseconds(TimerFrequency.Main).TimeOfDay))
            {
                reportSwitchWindow = false;
            }

            // when the adjusted switch time is in the past and no postpones are queued, the theme should be updated
            if (!state.PostponeManager.IsPostponed || init)
            {
                if (init) init = false;
                return new(reportSwitchWindow, new(SwitchSource.NightLightTrackerModule, state.NightLight.Requested, adjustedTime));
            }
            else
            {
                return new(reportSwitchWindow);
            }
        }

        public void DisableHook()
        {
            try
            {
                nightLightKeyWatcher.Stop();
                nightLightKeyWatcher.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not dispose of night light registry key watcher: ");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateNightLightState()
        {
            bool enabled = false;
            try
            {
                enabled = RegistryHandler.IsNightLightEnabled();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error retrieving night light enabled state:");
            }
            Theme newTheme = enabled ? Theme.Dark : Theme.Light;
            if (newTheme != nightLightState)
            {
                if (init) lastNightLightQueryTime = DateTime.Now.AddHours(-24);
                else lastNightLightQueryTime = DateTime.Now;
                nightLightState = newTheme;
                Logger.Info($"night light status enabled changed to {enabled}");
                bool isSkipNext = state.PostponeManager.GetSkipNextSwitchItem() != null;
                // if we are on the right theme and postpone is still enabled, we need to clear postpone on the next switch
                // As such we mark postpone for removal and take care of it on the next switch, allowing Fire()
                // If the postpone was cleared otherwise in the meantime, we also need to reset the queue postpone
                if (isSkipNext && !queuePostponeRemove)
                {
                    queuePostponeRemove = true;
                    state.NightLight.Requested = newTheme;
                    return;
                }
                else if (isSkipNext && queuePostponeRemove)
                {
                    queuePostponeRemove = false;
                    state.PostponeManager.RemoveSkipNextSwitch();
                }
                else if (queuePostponeRemove && !isSkipNext)
                {
                    queuePostponeRemove = false;
                }
                Master.Fire();
            }
        }

        public void EnableHook()
        {
            Logger.Info("night light governor selected");
            try
            {
                nightLightKeyWatcher = WMIHandler.CreateHKCURegistryValueMonitor(UpdateNightLightState, "Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\CloudStore\\\\Store\\\\DefaultAccount\\\\Current\\\\default$windows.data.bluelightreduction.bluelightreductionstate\\\\windows.data.bluelightreduction.bluelightreductionstate", "Data");
                nightLightKeyWatcher.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not start night light regkey monitor:");
            }

            UpdateNightLightState();
        }
    }
}
