using AutoDarkModeLib;
using AutoDarkModeSvc.Events;

namespace AutoDarkModeSvc.Interfaces;

public interface IAutoDarkModeGovernor
{
    abstract Governor Type { get; }
    /// <summary>
    /// Executes the governor logic
    /// </summary>
    /// <returns>A GovernorEvent containing information about the state of the governor</returns>
    GovernorEventArgs Run();
    /// <summary>
    /// Logic that should be called when a governor is enabled
    /// </summary>
    void EnableHook();
    /// <summary>
    /// Logic that should be called when a governor is disabled
    /// </summary>
    void DisableHook();

}
