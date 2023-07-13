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
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    internal class GovernorModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        GlobalState State { get; } = GlobalState.Instance();
        AdmConfigBuilder Builder { get; } = AdmConfigBuilder.Instance();
        DateTime LastSwitchWindow { get; set; }
        private IAutoDarkModeGovernor ActiveGovernor { get; set; }
        private bool NglInstantSwitchWindow { get; set; } = false;
        public GovernorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) 
        {
            Priority = 1;
        }


        public override string TimerAffinity => TimerName.Main;

        public override async Task Fire(object caller = null)
        {
            if (ActiveGovernor == null)
            {
                AutoManageGovernors(Builder.Config.Governor);
            }
            GovernorEventArgs result = ActiveGovernor.Run();
            if (result.InSwitchWindow && !State.SwitchApproach.ThemeSwitchApproaching)
            {
                LastSwitchWindow = DateTime.Now;
                // NIGHT GIGHT LOVERNOR specific code
                //
                // if no sunrise/sunset offset is configured and we have the night light governor active,
                // we will prevent normal timer ticks from setting a theme switch approaching state.
                // This is allowed, because the night light registry key update event will trigger the governor's fire right away.
                // As the switch window is used for letting modules know they need to start performing their tasks while on a timer,
                // having a switch window of 0 is not a problem if the State.SwitchApproach.TriggerDependencyModules() is immediately called.
                // That will force modules to perform their resume check tasks immediately.
                //
                // We achieve this by checking whether the active governor and caller of the fire method is the night light governor.
                // Then we check if the instant switch window functionality is enabled.
                // If so, we will briefly enable ThemeSwitchApproaching until modules have completed their resume check tasks.
                // If the caller is a night light governor, but no instant switch window flag is set, the timer should handle setting the switch window.
                // As such, we will enable the switch window as usual.
                if (ActiveGovernor is NightLightGovernor)
                {
                    if (caller is NightLightGovernor ngl)
                    {
                        NglInstantSwitchWindow = ngl.InstantSwitchWindow;
                        if (NglInstantSwitchWindow)
                        {
                            Logger.Debug("instant switch window");
                            State.SwitchApproach.ThemeSwitchApproaching = true;
                            await State.SwitchApproach.TriggerDependencyModules();
                            if (result.SwitchEventArgs != null) ThemeManager.RequestSwitch(result.SwitchEventArgs);
                            State.SwitchApproach.ThemeSwitchApproaching = false;
                        }
                    }
                    if (!NglInstantSwitchWindow)
                    {
                        Logger.Debug($"theme switch window is approaching");
                        State.SwitchApproach.ThemeSwitchApproaching = true;
                    }
                }
                else
                {
                    Logger.Debug($"theme switch window is approaching");
                    State.SwitchApproach.ThemeSwitchApproaching = true;
                }
            }
            else if (result.SwitchEventArgs != null)
            {
                ThemeManager.RequestSwitch(result.SwitchEventArgs);
            }
            if (!result.InSwitchWindow && State.SwitchApproach.ThemeSwitchApproaching)
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
