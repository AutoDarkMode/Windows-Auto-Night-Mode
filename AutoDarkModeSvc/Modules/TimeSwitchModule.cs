using System;
using AutoDarkModeSvc.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;
using AutoDarkModeSvc.Timers;
using System.Diagnostics.CodeAnalysis;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.Main;
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public TimeSwitchModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }

        public override void Fire()
        {
            Task.Run(() =>
            {
                ThemeManager.TimedSwitch(ConfigBuilder.Config);
            });
        }
    }
}
