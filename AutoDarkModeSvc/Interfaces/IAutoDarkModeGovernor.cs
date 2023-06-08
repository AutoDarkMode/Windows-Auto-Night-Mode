using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Interfaces
{
    public interface IAutoDarkModeGovernor
    {
        public abstract Governor Type { get; } 
        /// <summary>
        /// Executes the governor logic
        /// </summary>
        /// <returns>A GovernorEvent containing information about the state of the governor</returns>
        public GovernorEventArgs Run();
        /// <summary>
        /// Logic that should be called when a governor is enabled
        /// </summary>
        public void EnableHook();
        /// <summary>
        /// Logic that should be called when a governor is disabled
        /// </summary>
        public void DisableHook();

    }
}
