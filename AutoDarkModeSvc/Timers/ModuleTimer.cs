using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Timers;
using AutoDarkModeApp.Config;

namespace AutoDarkModeSvc.Timers
{
    class ModuleTimer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<IAutoDarkModeModule> Modules { get; set; }
        private Timer Timer { get; set; }
        public string Name { get; }
        private bool TickOnStart { get;  }

        /// <summary>
        /// A ModuleTimer runs with a preset interval and periodically call registered <see cref="IAutoDarkModeModule"/> modules
        /// </summary>
        /// <param name="interval">A timer interval to determine when <see cref="ModuleTimer.OnTimedEvent(object, ElapsedEventArgs)" should be invoked/></param>
        /// <param name="name">unique timer name</param>
        /// <param name="tickOnStart">set to true if the timer should tick immediately after starting instead of waiting one interval first</param>
        public ModuleTimer(int interval, string name, bool tickOnStart)
        {
            Name = name;
            Modules = new List<IAutoDarkModeModule>();
            Timer = new Timer
            {
                Interval = interval,
                Enabled = false,
                AutoReset = true                
            };
            TickOnStart = tickOnStart;
            Timer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // copying allows dynamic updates of the Module list since it can only be changed once every OnTimedEvent
            List<IAutoDarkModeModule> pollable = new List<IAutoDarkModeModule>(Modules);
            Logger.Debug($"{Name}timer signal received");
            pollable.ForEach(t =>
            {
                t.Poll();
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
                Modules.Add(module);
                Logger.Info($"registered module {module.Name} to timer {Name}");
            }
        }

        public void DeregisterModule(IAutoDarkModeModule module) 
        {
            if (Modules.Contains(module))
            {
                Modules.Remove(Modules.Find(m => m.Name == module.Name));
                Logger.Info($"deregistered module {module.Name} from timer {Name}");
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
            Logger.Info($"starting {Name} timer with {Timer.Interval} ms timer interval");
            Timer.Start();
            if (TickOnStart)
            {
                OnTimedEvent(this, EventArgs.Empty as ElapsedEventArgs);
            }            
        }

        public void Stop()
        {
            Logger.Info("shutting down {0} timer", Name);
            Timer.Stop();
        }

        public void Dispose()
        {
            Logger.Info("{0} timer disposed", Name);
            Timer.Dispose();
        }
    }
}
