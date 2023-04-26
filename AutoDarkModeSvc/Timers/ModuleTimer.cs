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
using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Timers;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeLib;

namespace AutoDarkModeSvc.Timers
{
    public class ModuleTimer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<IAutoDarkModeModule> Modules { get; set; }
        private Timer Timer { get; set; }
        public string Name { get; }
        private bool TickOnStart { get;  }

        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        /// <summary>
        /// A ModuleTimer runs with a preset interval and periodically call registered <see cref="IAutoDarkModeModule"/> modules
        /// </summary>
        /// <param name="interval">A timer interval to determine when <see cref="ModuleTimer.OnTimedEvent(object, ElapsedEventArgs)"/> should be invoked</param>
        /// <param name="name">unique timer name</param>
        public ModuleTimer(int interval, string name)
        {
            Name = name;
            Modules = new List<IAutoDarkModeModule>();
            Timer = new Timer
            {
                Interval = interval,
                Enabled = false,
                AutoReset = true
            };
            Timer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // copying allows dynamic updates of the Module list since it can only be changed once every OnTimedEvent
            List<IAutoDarkModeModule> ready = new(Modules);
            if (builder.Config.Tunable.DebugTimerMessage)
            {
                Logger.Debug($"{Name}timer signal received");
            }

            ready.ForEach(t =>
            {
                t.Fire();
            });
        }

        /// <summary>
        /// Register a new <see cref="IAutoDarkModeModule" module/>
        /// </summary>
        /// <param name="module"></param>
        public void RegisterModule(IAutoDarkModeModule module)
        {
            if (!Modules.Contains(module))
            {
                module.EnableHook();
                if (module.FireOnRegistration)
                {
                    module.Fire();
                }
                Modules.Add(module);
                Modules.Sort();
                Logger.Debug($"registered {module.Name} to timer {Name}");
            }
            // possible call OnTimedEvent here to reduce wait time after module has been added
            // maybe counters concurrency mitigation delay
        }

        public void DeregisterModule(IAutoDarkModeModule module)
        {
            if (Modules.Contains(module))
            {
                module.DisableHook();
                Modules.Remove(Modules.Find(m => m.Name == module.Name));
                Logger.Debug($"deregistered {module.Name} from timer {Name}");
            }
        }

        public void DeregisterModule(string moduleName)
        {
            IAutoDarkModeModule module = Modules.Find(m => m.Name == moduleName);
            if (module != null) DeregisterModule(module);
        }

        public List<IAutoDarkModeModule> GetModules()
        {
            return new List<IAutoDarkModeModule>(Modules);
        }

        public void Start()
        {
            Logger.Trace($"starting {Name} timer with {Timer.Interval} ms timer interval");
            Timer.Start();
            if (TickOnStart)
            {
                OnTimedEvent(this, EventArgs.Empty as ElapsedEventArgs);
            }
        }

        public void Stop()
        {
            Logger.Trace("shutting down {0} timer", Name);
            Timer.Stop();
        }

        public void Dispose()
        {
            Timer.Dispose();
            Logger.Trace("{0} timer disposed", Name);
        }
    }
}
