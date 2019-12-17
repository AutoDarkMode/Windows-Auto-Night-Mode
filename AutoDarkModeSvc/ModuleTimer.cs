using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.Timers;

namespace AutoDarkModeSvc
{
    class ModuleTimer
    {
        private List<IAutoDarkModeModule> Tasks { get; set; }
        private Timer Timer { get; set; }
        public ModuleTimer(int interval)
        {
            Tasks = new List<IAutoDarkModeModule>();
            Timer = new Timer
            {
                Interval = 10000,
                Enabled = false,
                AutoReset = true
                
            };
            Timer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("{0}: Timer Signal triggered", e.SignalTime);
            Tasks.ForEach(t =>
            {
                t.RunTask();
            });
        }

        public void RegisterModule(IAutoDarkModeModule module)
        {
            Tasks.Add(module);
        }

        public void DeregisterModule(IAutoDarkModeModule module) 
        {
            Tasks.Remove(Tasks.Find(m => m.Name == module.Name));
        }

    }
}
