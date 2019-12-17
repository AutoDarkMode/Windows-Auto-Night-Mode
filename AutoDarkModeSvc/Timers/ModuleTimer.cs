using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Timers;

namespace AutoDarkModeSvc.Timers
{
    class ModuleTimer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<IAutoDarkModeModule> Modules { get; set; }
        private Timer Timer { get; set; }
        public ModuleTimer(int interval)
        {
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
            Logger.Debug("{0}: Timer Signal triggered", e.SignalTime);
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
            Timer.Start();
        }

        public void Stop()
        {
            Timer.Stop();
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}
