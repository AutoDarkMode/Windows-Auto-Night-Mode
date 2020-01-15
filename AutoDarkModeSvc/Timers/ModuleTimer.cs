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

        public void RegisterModule(IAutoDarkModeModule module)
        {
            Modules.Add(module);
            Logger.Info($"registered module {module.Name} to timer {Name}");
        }

        public void DeregisterModule(IAutoDarkModeModule module) 
        {
            Modules.Remove(Modules.Find(m => m.Name == module.Name));
            Logger.Info($"deregistered module {module.Name} to timer {Name}");
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
