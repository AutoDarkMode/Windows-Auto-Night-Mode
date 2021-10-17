using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeSvc.Config;

namespace AutoDarkModeSvc.Modules
{
    public interface IAutoDarkModeModule : IEquatable<IAutoDarkModeModule>, IComparable<IAutoDarkModeModule>
    {
        /// <summary>
        /// Polling method to be periodically called by <see cref="AutoDarkModeSvc.Timers.ModuleTimer.OnTimedEvent(object, System.Timers.ElapsedEventArgs)"/>
        /// </summary>
        public void Fire();
        /// <summary>
        /// Performs cleanup operations before a module is deregistered
        /// </summary>
        public void Cleanup();
        /// <summary>
        /// Unique timer identification
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Unique timer name for automatic module registration and deregistration
        /// </summary>
        public string TimerAffinity { get; }
        /// <summary>
        /// Determines whether a moudle should fire when it is registered to a timer
        /// </summary>
        public bool FireOnRegistration { get; }
        /// <summary>
        /// Denotes in which order the module should fire
        /// </summary>
        public int Priority { get; }
    }
}
