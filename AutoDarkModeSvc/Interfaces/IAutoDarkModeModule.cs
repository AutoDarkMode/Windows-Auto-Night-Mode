#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeSvc.Monitors;

namespace AutoDarkModeSvc.Modules
{
    public interface IAutoDarkModeModule : IEquatable<IAutoDarkModeModule>, IComparable<IAutoDarkModeModule>
    {
        /// <summary>
        /// Polling method to be periodically called by <see cref="AutoDarkModeSvc.Timers.ModuleTimer.OnTimedEvent(object, System.Timers.ElapsedEventArgs)"/>
        /// </summary>
        public Task Fire(object caller = null);
        /// <summary>
        /// Performs operations that should be called upon instantiation
        /// </summary>
        public void EnableHook();
        /// <summary>
        /// Performs cleanup operations before a module is deregistered
        /// </summary>
        public void DisableHook();
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
