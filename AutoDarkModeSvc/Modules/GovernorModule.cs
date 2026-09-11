using System;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Governors;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules;

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

    public async override Task Fire(object caller = null)
    {
        if (ActiveGovernor == null)
        {
            AutoManageGovernors(Builder.Config.Governor);
        }
        GovernorEventArgs result = ActiveGovernor.Run();
        if (result.InSwitchWindow && !State.SwitchApproach.ThemeSwitchApproaching)
        {
            LastSwitchWindow = DateTime.Now;
            // The switch window tells dependency modules that they need to start performing their tasks,
            // usually spanning a few main timer ticks so they can postpone the switch before it is due.
            //
            // A governor that cannot predict its switch time (night light without offsets, ambient light sensor)
            // reports a window of length zero at the very moment it decides to switch. Such a governor only
            // reports it on the fire it caused itself, never on a timer tick.
            // The dependency modules then have to run right here, otherwise they never get the chance to
            // postpone, and the switch is requested once they are done.
            if (result.InstantSwitchWindow)
            {
                Logger.Debug("instant switch window");
                State.SwitchApproach.ThemeSwitchApproaching = true;
                await State.SwitchApproach.TriggerDependencyModules();
                if (result.SwitchEventArgs != null) ThemeManager.RequestSwitch(result.SwitchEventArgs);
                State.SwitchApproach.ThemeSwitchApproaching = false;
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
            else if (newGovernor == Governor.AmbientLight)
            {
                ActiveGovernor = new AmbientLightGovernor(this);
            }
            else if (newGovernor == Governor.Power)
            {
                ActiveGovernor = new PowerGovernor(this);
            }
            ActiveGovernor.EnableHook();
        }
    }
}
