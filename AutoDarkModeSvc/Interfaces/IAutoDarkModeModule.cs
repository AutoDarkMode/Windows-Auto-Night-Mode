using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeApp.Config;

namespace AutoDarkModeSvc.Modules
{
    interface IAutoDarkModeModule : IEquatable<IAutoDarkModeModule>
    {
        /// <summary>
        /// Polling method to be periodically called by <see cref="AutoDarkModeSvc.Timers.ModuleTimer.OnTimedEvent(object, System.Timers.ElapsedEventArgs)"/>
        /// </summary>
        public void Poll();
        /// <summary>
        /// Unique timer identification
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Unique timer name for automatic module registration and deregistration
        /// </summary>
        public string TimerAffinity { get;  }
    }
}
