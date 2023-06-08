using AutoDarkModeLib.Configs;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using AutoDarkModeSvc.Governors;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Events;
using static System.Windows.Forms.AxHost;

namespace AutoDarkModeSvc.Modules
{
    internal class GovernorModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        GlobalState State { get; } = GlobalState.Instance();
        AdmConfigBuilder Builder { get; } = AdmConfigBuilder.Instance();
        DateTime LastSwitchWindow { get; set; }
        private IAutoDarkModeGovernor ActiveGovernor { get; set; }
        public GovernorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) 
        {
            Priority = 1;
        }

        public override string TimerAffinity => TimerName.Main;

        public override void Fire()
        {
            if (ActiveGovernor == null)
            {
                AutoManageGovernors(Builder.Config.Governor);
            }
            GovernorEventArgs result = ActiveGovernor.Run();
            if (result.InSwitchWindow && !State.SwitchApproach.ThemeSwitchApproaching)
            {
                Logger.Debug($"theme switch window is approaching");
                LastSwitchWindow = DateTime.Now;
                State.SwitchApproach.ThemeSwitchApproaching = true;
                if (ActiveGovernor is NightLightGovernor)
                {
                    State.SwitchApproach.TriggerDependencyModules();
                    Logger.Debug($"theme switch approach window has passed");
                    State.SwitchApproach.ThemeSwitchApproaching = false;
                }
            }
            else if (result.SwitchEventArgs != null)
            {
                ThemeManager.RequestSwitch(result.SwitchEventArgs);
            }
            if (ActiveGovernor is not NightLightGovernor && !result.InSwitchWindow && State.SwitchApproach.ThemeSwitchApproaching)
            {
                Logger.Debug($"theme switch approach window has passed");
                State.SwitchApproach.ThemeSwitchApproaching = false;
            }
        }

        public void AutoManageGovernors(Governor newGovernor)
        {
            if (ActiveGovernor?.Type != newGovernor)
            {
                // not sure about this yet, but the idea is to reset the theme switch approaching flat in case the user config changes
                State.SwitchApproach.ThemeSwitchApproaching = false;
                ActiveGovernor?.DisableHook();
                if (newGovernor == Governor.Default)
                {
                    ActiveGovernor = new TimeSwitchGovernor();
                }
                else if (newGovernor == Governor.NightLight)
                {
                    ActiveGovernor = new NightLightGovernor(this);
                }
                ActiveGovernor.EnableHook();
            }
        }
    }
}
