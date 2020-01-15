using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    class ModuleWardenModule : IAutoDarkModeModule
    {
        public string Name { get; }
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }
        private List<ModuleTimer> Timers { get; }
        public string TimerAffinity { get; } = TimerName.Main;
        public ModuleWardenModule(string name, List<ModuleTimer> timers)
        {
            Name = name;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
            Timers = timers;
        }

        public void Poll()
        {
            AutoDarkModeConfig config = ConfigBuilder.Config;
            AutoManageModule("TimeSwitch", typeof(TimeSwitchModule), config.Enabled);
            AutoManageModule("GeopositionUpdate", typeof(GeopositionUpdateModule), !config.Location.Disabled);
            AutoManageModule("ConfigRefresh", typeof(ConfigRefreshModule), true);
        }

        private void AutoManageModule(string moduleName, Type moduleType, bool condition)
        {
            if (typeof(IAutoDarkModeModule).IsAssignableFrom(moduleType))
            {
                List<IAutoDarkModeModule> registeredModules = new List<IAutoDarkModeModule>();
                Timers.ForEach(t => registeredModules.AddRange(t.GetModules()));

                if (condition)
                {
                    if (registeredModules.FindIndex(m => m.Name == moduleName) == -1)
                    {
                        IAutoDarkModeModule module = Activator.CreateInstance(moduleType, moduleName) as IAutoDarkModeModule;
                        var timer = Timers.Find(t => t.Name == module.TimerAffinity);
                        if (timer != null)
                        {
                            timer.RegisterModule(module);
                        }
                    }
                }
                else
                {
                    var module = registeredModules.Find(m => m.Name == moduleName);
                    if (module != null)
                    {
                        var timer = Timers.Find(t => t.Name == module.TimerAffinity);
                        if (timer != null)
                        {
                            timer.DeregisterModule(module);
                        }
                    }
                }
            }            
        }
    }
}
