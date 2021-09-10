using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    class WardenModule : AutoDarkModeModule
    {
        private AdmConfigBuilder ConfigBuilder { get; }
        private GlobalState State { get; }
        private List<ModuleTimer> Timers { get; }
        public override string TimerAffinity { get; } = TimerName.Main;


        /// <summary>
        /// Instantiates a new ModuleWardenModule.
        /// This module registers and deregisters modules automatically based on the AutoDarkModeConfiguration
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public WardenModule(string name, List<ModuleTimer> timers, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AdmConfigBuilder.Instance();
            State = GlobalState.Instance();
            State.SetWarden(this);
            Timers = timers;
            Priority = 1;
        }

        public override void Fire()
        {
            AdmConfig config = ConfigBuilder.Config;
            AutoManageModule(typeof(GeopositionUpdateModule).Name, typeof(GeopositionUpdateModule), true, config.Location.Enabled);
            AutoManageModule(typeof(TimeSwitchModule).Name, typeof(TimeSwitchModule), true, config.AutoThemeSwitchingEnabled && !State.PostponeSwitch);
            AutoManageModule(typeof(BlueLightSwitchModule).Name, typeof(BlueLightSwitchModule), true, config.BlueLightSwitchingEnabled && !State.PostponeSwitch);
            AutoManageModule(typeof(ThemeUpdateModule).Name, typeof(ThemeUpdateModule), true, !config.ClassicMode);
            AutoManageModule(typeof(GPUMonitorModuleV2).Name, typeof(GPUMonitorModuleV2), true, config.GPUMonitoring.Enabled);
            AutoManageModule(typeof(EventModule).Name, typeof(EventModule), true, config.Events.Enabled);
        }

        /// <summary>
        /// Automatically manages the registration and deregistration of modules based on the current configuration state
        /// </summary>
        /// <param name="moduleName">unique name of the module to be managed</param>
        /// <param name="moduleType">Type of a class implementing the <see cref="IAutoDarkModeModule"/> interface</param>
        /// <param name="fireOnRegistration">Determines whether a module should fire upon registration to a timer</param>
        /// <param name="condition">condition whether a module should be registered or deregistered</param>
        private void AutoManageModule(string moduleName, Type moduleType, bool fireOnRegistration, bool condition)
        {
            // check if the type impplements the interface for compatibility with a ModuleTimer
            if (typeof(IAutoDarkModeModule).IsAssignableFrom(moduleType))
            {
                // register a module if the condition has been set to true (should be predetermined in Poll() before calling this method
                if (condition)
                {
                    IAutoDarkModeModule module = Activator.CreateInstance(moduleType, moduleName, fireOnRegistration) as IAutoDarkModeModule;
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
