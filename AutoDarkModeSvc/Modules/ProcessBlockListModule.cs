using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
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
    private bool IsPostponing => State.PostponeManager.Get(Name) != null;
    private AdmConfigBuilder ConfigBuilder { get; }

    public ProcessBlockListModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
    {
        State = GlobalState.Instance();
        ConfigBuilder = AdmConfigBuilder.Instance();
    }

    public override string TimerAffinity { get; } = TimerName.Main;

    public override void Fire()
    {
        if (!ConfigBuilder.Config.ProcessBlockList.Enabled)
        {
            RemovePostpone();
            Logger.Debug("No processes are excluded, skipping checks");
            return;
        }

        // While postponing, continue checking
        if (!IsPostponing && !State.ThemeSwitchApproaching)
        {
            Logger.Debug("It's still a while until a time based switch would happen, skip checking processes");
            return;
        }

        Task.Run(() =>
        {
            if (TestRunningProcesses())
            {
                Postpone();
            }
            else
            {
                RemovePostpone();
            }
        });
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

    public override void EnableHook()
    {
        State.AddSwitchApproachDependency(GetType().Name);
        base.EnableHook();
    }

    public override void DisableHook()
    {
        Logger.Info("Removing any leftover process block list postones");
        RemovePostpone();
        State.RemoveSwitchApproachDependency(GetType().Name);
        base.DisableHook();
    }

    /// <summary>
    /// Add this module's postpone
    /// </summary>
    /// <returns>True if a new postpone was added</returns>
    private bool Postpone()
    {
        Logger.Debug("Adding postpone from process exclusion");
        return State.PostponeManager.Add(new PostponeItem(Name));
    }

    /// <summary>
    /// Remove this module's postpone
    /// </summary>
    /// <returns>True if a postpone was removed </returns>
    private bool RemovePostpone()
    {
        Logger.Info("Clearing postpone from process exclusion");
        return State.PostponeManager.Remove(Name);
    }
}