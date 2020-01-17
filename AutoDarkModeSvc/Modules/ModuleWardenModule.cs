using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    class ModuleWardenModule : AutoDarkModeModule
    {
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }
        private List<ModuleTimer> Timers { get; }
        public override string TimerAffinity { get; } = TimerName.Main;


        /// <summary>
        /// Instantiates a new ModuleWardenModule.
        /// This module registers and deregisters modules automatically based on the AutoDarkModeConfiguration
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public ModuleWardenModule(string name, List<ModuleTimer> timers)
        {
            Name = name;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
            Timers = timers;
        }

        public override void Fire()
        {
            AutoDarkModeConfig config = ConfigBuilder.Config;
            AutoManageModule(typeof(TimeSwitchModule).Name, typeof(TimeSwitchModule), config.Enabled);
            AutoManageModule(typeof(GeopositionUpdateModule).Name, typeof(GeopositionUpdateModule), config.Location.Enabled);
            //AutoManageModule(typeof(ConfigLoadModule).Name, typeof(ConfigLoadModule), true);
        }

        /// <summary>
        /// Automatically manages the registration and deregistration of modules based on the current configuration state
        /// </summary>
        /// <param name="moduleName">unique name of the module to be managed</param>
        /// <param name="moduleType">Type of a class implementing the <see cref="IAutoDarkModeModule"/> interface</param>
        /// <param name="condition">condition whether a module should be registered or deregistered</param>
        private void AutoManageModule(string moduleName, Type moduleType, bool condition)
        {
            // check if the type impplements the interface for compatibility with a ModuleTimer
            if (typeof(IAutoDarkModeModule).IsAssignableFrom(moduleType))
            {
                // register a module if the condition has been set to true (should be predetermined in Poll() before calling this method
                if (condition)
                {
                    IAutoDarkModeModule module = Activator.CreateInstance(moduleType, moduleName) as IAutoDarkModeModule;
                    var timer = Timers.Find(t => t.Name == module.TimerAffinity);
                    if (timer != null)
                    {
                        timer.RegisterModule(module);
                    }
                }
                else
                {
                    Timers.ForEach(t => t.DeregisterModule(moduleName));
                }
            }            
        }
    }
}
