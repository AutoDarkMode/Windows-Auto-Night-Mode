using System;
using System.Diagnostics;
using System.Linq;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules;

/// <summary>
/// This Module postpones theme switches if processes with a certain name are running.
/// The process names are configured in <see cref="AutoDarkModeLib.Configs.ProcessBlockList"/> 
/// </summary>
///
/// <seealso cref="AutoDarkModeApp.Pages.PageSwitchModes"/>
/// <seealso cref="AutoDarkModeLib.Configs.ProcessBlockList"/>
public class ProcessBlockListModule : AutoDarkModeModule
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private GlobalState State { get; }
    private bool IsPostponing => State.PostponeManager.Get(ExcludedProcessIsRunningReason)!=null;
    private AdmConfigBuilder ConfigBuilder { get; }

    public ProcessBlockListModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
    {
        State = GlobalState.Instance();
        ConfigBuilder = AdmConfigBuilder.Instance();
    }

    private const string ExcludedProcessIsRunningReason = "Blocked process is running";

    public override string TimerAffinity { get; } = TimerName.Main;

    public override void Fire()
    {
        if (ConfigBuilder.Config.ProcessBlockList.ProcessNames.Count == 0)
        {
            RemovePostpone();
            Logger.Debug("No processes are excluded, skipping checks");
            return;
        }

        // While postponing, continue checking
        if (IsPostponing && !IsAboutToSwitchThemes(1))
        {
            Logger.Debug("It's still a while until a time based switch would happen, skip checking processes");
            return;
        }

        if (TestRunningProcesses())
        {
            Postpone();
        }
        else
        {
            RemovePostpone();
        }
    }

    /// <summary>
    /// Tests if any of the blocked processes are currently running
    /// </summary>
    /// <returns>True if any of the processes are running</returns>
    private bool TestRunningProcesses()
    {
        var activeProcesses = Process
            .GetProcesses()
            .Select(p =>
            {
                try
                {
                    if (p.MainWindowHandle != 0)
                    {
                        return p.ProcessName;
                    }
                }
                catch (Exception e)
                {
                    Logger.Debug(e, "");
                }

                return null;
            })
            .Where(name => name != null)
            .ToHashSet();

        return ConfigBuilder.Config.ProcessBlockList.ProcessNames.Any(p => activeProcesses.Contains(p));
    }


    /// <summary>
    /// Tests if the current time is before a planned theme switch
    /// </summary>
    /// <param name="grace">Minutes before the planned theme switch</param>
    /// <returns>Returns true if the current time is within grace minutes before a time based theme switch</returns>
    private bool IsAboutToSwitchThemes(int grace)
    {
        var sunriseMonitor = ConfigBuilder.Config.Sunrise;
        var sunsetMonitor = ConfigBuilder.Config.Sunset;
        var now = DateTime.Now;

        if (ConfigBuilder.Config.Location.Enabled)
        {
            LocationHandler.GetSunTimes(ConfigBuilder, out sunriseMonitor, out sunsetMonitor);
        }

        var isShortlyBeforeSwitchTime =
            Helper.NowIsBetweenTimes(sunriseMonitor.AddMinutes(-Math.Abs(grace)).TimeOfDay, sunriseMonitor.TimeOfDay) ||
            Helper.NowIsBetweenTimes(sunsetMonitor.AddMinutes(-Math.Abs(grace)).TimeOfDay, sunsetMonitor.TimeOfDay);

        return isShortlyBeforeSwitchTime;
    }


    public override void DisableHook()
    {
        Logger.Info("Removing any leftover process block list postones");
        Postpone();
        base.DisableHook();
    }

    /// <summary>
    /// Add this module's postpone
    /// </summary>
    /// <returns>True if a new postpone was added</returns>
    private bool Postpone()
    {
        Logger.Debug("Adding postpone from process exclusion");
        return State.PostponeManager.Add(new PostponeItem(ExcludedProcessIsRunningReason));
    }

    /// <summary>
    /// Remove this module's postpone
    /// </summary>
    /// <returns>True if a postpone was removed </returns>
    private bool RemovePostpone()
    {
        Logger.Info("Clearing postpone from process exclusion");
        return State.PostponeManager.Remove(ExcludedProcessIsRunningReason);
    }
}