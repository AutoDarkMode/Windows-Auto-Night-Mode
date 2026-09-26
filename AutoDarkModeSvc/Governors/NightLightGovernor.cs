using System;
using System.Management;
using System.Runtime.CompilerServices;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Governors;

public class NightLightGovernor : IAutoDarkModeGovernor
{
    public Governor Type => Governor.NightLight;
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private DateTime lastNightLightQueryTime = DateTime.Now;
    private Theme nightLightState = Theme.Unknown;
    private ManagementEventWatcher nightLightKeyWatcher;
    private GlobalState state = GlobalState.Instance();
    private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    private bool init = true; // note: unused?
    private bool queuePostponeRemove = false;
    private bool switchQueuedButNotRequested = false;
    private IAutoDarkModeModule Master { get; }

    /// <summary>
    /// True while the configured offset for the upcoming switch is zero. The registry key event then arrives
    /// at the exact time of the switch, so there is no time span left in which the timer could report a
    /// switch window ahead of it.
    /// </summary>
    private bool instantSwitchWindow = false;

    /// <summary>
    /// Set when the governor itself triggers a switch, consumed by the next <see cref="Run"/>.
    /// Ensures the instant switch window is only reported for the fire the governor caused,
    /// and not again on the following timer ticks.
    /// </summary>
    private bool instantSwitchWindowPending = false;

    public NightLightGovernor(IAutoDarkModeModule master)
    {
        Master = master;
    }


    public GovernorEventArgs Run()
    {
        DateTime adjustedTime;
        DateTime adjustedSwitchWindowStart;
        DateTime adjustedSwitchWindowEnd;

        //apply offsets to the latest available switch times
        switch (nightLightState)
        {
            case Theme.Dark:
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunsetOffsetMin);
                adjustedSwitchWindowStart = adjustedTime.AddMilliseconds(-TimerFrequency.Main);
                adjustedSwitchWindowEnd = adjustedTime;
                break;
            default:
                adjustedTime = lastNightLightQueryTime.AddMinutes(builder.Config.Location.SunriseOffsetMin);
                adjustedSwitchWindowStart = adjustedTime.AddMilliseconds(-TimerFrequency.Main);
                adjustedSwitchWindowEnd = adjustedTime;
                break;
        }

        DateTime callTime = DateTime.Now;

        // if the switch time is in the future, we need to set the global night light theme to the opposite of the internally tracked one
        // Otherwise the incorrect theme will show up
        if (DateTime.Compare(adjustedTime, callTime) > 0 && !init)
        {
            if (!switchQueuedButNotRequested)
            {
                switch (nightLightState)
                {
                    case Theme.Light:
                        state.NightLight.Requested = Theme.Dark;
                        break;
                    case Theme.Dark:
                        state.NightLight.Requested = Theme.Light;
                        break;
                }
                switchQueuedButNotRequested = true;
            }
        }
        else if (state.NightLight.Requested != nightLightState)
        {
            switchQueuedButNotRequested = false;
            state.NightLight.Requested = nightLightState;
        }

        // if auto switch notify is enabled and we are approaching the switch window, we need to show a notification
        if (builder.Config.AutoSwitchNotify.Enabled && !init && state.PostponeManager.Get(Helper.PostponeItemSessionLock) == null)
        {
            if (!state.PostponeManager.IsGracePeriod && Helper.NowIsBetweenTimes(adjustedSwitchWindowStart.TimeOfDay, adjustedSwitchWindowEnd.TimeOfDay)
                && state.NightLight.Requested != state.InternalTheme)
            {
                ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                return new(true);
            }
        }

        bool reportSwitchWindow = state.SwitchApproach.DependenciesPresent && !init;

        // the pending flag is a one shot, so it has to be consumed on every run
        bool instantWindow = instantSwitchWindowPending;
        instantSwitchWindowPending = false;

        if (reportSwitchWindow)
        {
            if (instantSwitchWindow)
            {
                // there is no time span the timer could report, so only the registry key event may open the window
                reportSwitchWindow = instantWindow;
            }
            else if (!Helper.NowIsBetweenTimes(adjustedSwitchWindowStart.TimeOfDay, adjustedSwitchWindowEnd.TimeOfDay))
            {
                // reporting is enabled, but we are not in the switch window
                reportSwitchWindow = false;
            }
        }

        if (init) init = false;
        return new(reportSwitchWindow, new(SwitchSource.NightLightTrackerModule, state.NightLight.Requested, adjustedTime), instantSwitchWindow: instantWindow);
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

            // instantSwitchWindow is used to prevent the timer from duplicating the switch window operations when the event
            // should be responsible for it
            instantSwitchWindow = nightLightState switch
            {
                Theme.Dark when builder.Config.Location.SunsetOffsetMin == 0 => true,
                Theme.Light when builder.Config.Location.SunriseOffsetMin == 0 => true,
                _ => false,
            };

            Logger.Info($"night light status enabled changed to {enabled}");
            bool isSkipNext = state.PostponeManager.GetSkipNextSwitchItem() != null;
            // if we are on the right theme and postpone is still enabled, we need to clear postpone on the next switch
            // As such we mark postpone for removal and take care of it on the next switch, allowing Fire()
            // If the postpone was cleared otherwise in the meantime, we also need to reset the queue postpone
            if (isSkipNext && !queuePostponeRemove)
            {
                queuePostponeRemove = true;
                state.NightLight.Requested = newTheme;
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
            instantSwitchWindowPending = instantSwitchWindow;
            Master.Fire(this);
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
