using System.Diagnostics;
using System.Linq;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules;

public class ProcessBlockList : AutoDarkModeModule
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private GlobalState State { get; }
    private AdmConfigBuilder ConfigBuilder { get; }

    public ProcessBlockList(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
    {
        State = GlobalState.Instance();
        ConfigBuilder = AdmConfigBuilder.Instance();
    }

    private const string ExcludedProcessIsRunningReason = "Excluded Process is running";

    public override string TimerAffinity { get; } = TimerName.Main;

    public override void Fire()
    {
        if (ConfigBuilder.Config.ProcessBlockList.ProcessNames.Count == 0)
        {
            Logger.Info("No processes are excluded, skipping checks");
            return;
        }

        var activeProcesses = Process
            .GetProcesses()
            .Where(p => p.MainWindowHandle != 0)
            .Select(p => p.ProcessName)
            .ToHashSet();

        if (ConfigBuilder.Config.ProcessBlockList.ProcessNames.Any(p => activeProcesses.Contains(p)))
        {
            Logger.Info("Adding postpone from process exclusion");
            State.PostponeManager.Add(new PostponeItem(ExcludedProcessIsRunningReason));
        }
        else
        {
            Logger.Info("Clearing postpone from process exclusion");
            State.PostponeManager.Remove(ExcludedProcessIsRunningReason);
        }
    }

    public override void DisableHook()
    {
        Logger.Info("Removing any leftover process exclusion postone");
        State.PostponeManager.Remove(ExcludedProcessIsRunningReason);
        base.DisableHook();
    }
}