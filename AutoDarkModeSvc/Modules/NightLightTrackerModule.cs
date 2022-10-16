using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    internal class NightLightTrackerModule : AutoDarkModeModule
    {
        public override string TimerAffinity => TimerName.Main;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private DateTime lastNightLightQueryTime = DateTime.Now;
        private Theme requestedTheme = Theme.Unknown;
        private static ManagementEventWatcher nightLightKeyWatcher;
        private GlobalState state = GlobalState.Instance();
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool init = true;
        private bool queuePostponeRemove = false;
        bool notified = false;

        public NightLightTrackerModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) { }


        public override void Fire()
        {
            DateTime adjustedTime;
            if (requestedTheme == Theme.Dark)
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunsetOffsetMin);
            }
            else
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunriseOffsetMin);
            }

            if (DateTime.Compare(adjustedTime, DateTime.Now) > 0 && !init)
            {
                if (requestedTheme == Theme.Light) state.NightLight.Current = Theme.Dark;
                else if (requestedTheme == Theme.Dark) state.NightLight.Current = Theme.Light;
            }
            else if (state.NightLight.Current != requestedTheme)
            {
                state.NightLight.Current = requestedTheme;
            }

            if (builder.Config.AutoSwitchNotify.Enabled && !init && state.PostponeManager.Get(Helper.PostponeItemSessionLock) == null)
            {
                if (!notified && Helper.NowIsBetweenTimes(adjustedTime.AddMinutes(-1).TimeOfDay, adjustedTime.AddMinutes(1).TimeOfDay)
                    && state.NightLight.Current != state.RequestedTheme)
                {
                    ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                    notified = true;
                }
                else if (notified && DateTime.Compare(DateTime.Now, adjustedTime.AddMinutes(1)) > 0) notified = false;
            }

            // when the adjusted switch time is in the past and no postpones are queued, the theme should be updated
            if (!state.PostponeManager.IsPostponed || init)
            {
                if (init) init = false;

                Task.Run(() =>
                {
                    ThemeManager.RequestSwitch(new(SwitchSource.NightLightTrackerModule, state.NightLight.Current, adjustedTime));
                });
            }
            return;
        }

        public override void DisableHook()
        {
            base.DisableHook();
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
            if (newTheme != requestedTheme)
            {
                lastNightLightQueryTime = DateTime.Now;
                requestedTheme = newTheme;
                Logger.Info($"night light status enabled changed to {enabled}");
                bool isSkipNext = state.PostponeManager.GetSkipNextSwitchItem() != null;
                // if we are on the right theme and postpone is still enabled, we need to clear postpone on the next switch
                // As such we mark postpone for removal and take care of it on the next switch, allowing Fire()
                // If the postpone was cleared otherwise in the meantime, we also need to reset the queue postpone
                if (isSkipNext && !queuePostponeRemove)
                {
                    queuePostponeRemove = true;
                    state.NightLight.Current = newTheme;
                    return;
                }
                else if (isSkipNext && queuePostponeRemove)
                {
                    queuePostponeRemove = false;
                    state.PostponeManager.RemoveUserClearablePostpones();
                }
                else if (queuePostponeRemove && !isSkipNext)
                {
                    queuePostponeRemove = false;
                }
                Fire();
            }
        }

        public override void EnableHook()
        {
            base.EnableHook();
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
