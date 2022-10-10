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

        public NightLightTrackerModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) { }


        public override void Fire()
        {
            DateTime adjustedTime;
            if (state.NightLight.Current == Theme.Dark)
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunsetOffsetMin);
            }
            else
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunriseOffsetMin);
            }

            // when the adjusted switch time is in the past and no postpones are queued, the theme should be updated
            if ((!state.PostponeManager.IsPostponed && DateTime.Compare(adjustedTime, DateTime.Now) <= 0) || init)
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
            bool enabled = RegistryHandler.IsNightLightEnabled();
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
                    state.PostponeManager.RemoveAllManualPostpones();
                }
                else if (queuePostponeRemove && !isSkipNext)
                {
                    queuePostponeRemove = false;
                }
                state.NightLight.Current = newTheme;
                Fire();
            }
        }

        public override void EnableHook()
        {
            base.EnableHook();
            nightLightKeyWatcher = WMIHandler.CreateHKCURegistryValueMonitor(UpdateNightLightState, "Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\CloudStore\\\\Store\\\\DefaultAccount\\\\Current\\\\default$windows.data.bluelightreduction.bluelightreductionstate\\\\windows.data.bluelightreduction.bluelightreductionstate", "Data");
            nightLightKeyWatcher.Start();
            UpdateNightLightState();
        }
    }
}
