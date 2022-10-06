using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    internal class NightLightTrackerModule : AutoDarkModeModule
    {
        public override string TimerAffinity => TimerName.Main;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private DateTime lastNightLightQueryTime = DateTime.Now;
        private static ManagementEventWatcher nightLightKeyWatcher;
        private GlobalState state = GlobalState.Instance();
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public NightLightTrackerModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            nightLightKeyWatcher = WMIHandler.CreateHKCURegistryValueMonitor(UpdateNightLightState, "Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\CloudStore\\\\Store\\\\DefaultAccount\\\\Current\\\\default$windows.data.bluelightreduction.bluelightreductionstate\\\\windows.data.bluelightreduction.bluelightreductionstate", "Data");
            nightLightKeyWatcher.Start();
            UpdateNightLightState();
        }


        public override void Fire()
        {
            DateTime adjustedTime;
            if (state.NightLightActiveTheme == Theme.Dark)
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunsetOffsetMin);
            }
            else
            {
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunriseOffsetMin);
            }

            // when the adjusted switch time is in the past and no postpones are queued, the theme should be updated
            if (!state.PostponeManager.IsPostponed && DateTime.Compare(adjustedTime, DateTime.Now) <= 0)
            {
                Task.Run(() =>
                {
                    ThemeManager.RequestSwitch(new(SwitchSource.NightLightTrackerModule, state.NightLightActiveTheme, adjustedTime));
                });
            }
            return;
        }

        public override void Cleanup()
        {
            base.Cleanup();
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

        public void UpdateNightLightState()
        {
            lastNightLightQueryTime = DateTime.Now;
            Theme newTheme = RegistryHandler.IsNightLightEnabled() ? Theme.Dark : Theme.Light;
            if (newTheme != state.NightLightActiveTheme)
            {
                state.NightLightActiveTheme = newTheme;
                Fire();
            }
        }
    }
}
