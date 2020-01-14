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
        private string Name { get; }
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

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Logger.Debug("timer signal received");
            Modules.ForEach(t =>
            {
                t.Poll();
            });
        }

        public void RegisterModule(IAutoDarkModeModule module)
        {
            Modules.Add(module);
        }

        public void DeregisterModule(IAutoDarkModeModule module) 
        {
            Modules.Remove(Modules.Find(m => m.Name == module.Name));
        }

        public void Start()
        {
            Logger.Info($"starting {Name} timer with {Timer.Interval} ms timer interval");
            Timer.Start();
            OnTimedEvent(this, EventArgs.Empty as ElapsedEventArgs);
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
