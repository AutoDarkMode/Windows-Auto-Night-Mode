using System;
using AutoDarkModeSvc.Config;
using System.Threading.Tasks;
using AutoDarkModeSvc.Timers;
using System.Diagnostics.CodeAnalysis;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.Modules
{
    class BlueLightSwitchModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.Main;
        private AdmConfigBuilder ConfigBuilder { get; }

        /// <summary>
        /// Instantiates a new BlueLightSwitchModule.
        /// This module switches themes based on blue light filter
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public BlueLightSwitchModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AdmConfigBuilder.Instance();
        }

        public override void Fire()
        {
            Task.Run(() =>
            {
                GlobalState.Instance().CurrentBluelight = RegistryHandler.GetBluelightEnabled();
                ThemeManager.BlueLightSwitch(ConfigBuilder);
            });
        }
    }
}