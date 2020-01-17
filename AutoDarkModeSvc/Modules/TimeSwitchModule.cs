using System;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Timers;
using System.Diagnostics.CodeAnalysis;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.IO;

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public TimeSwitchModule(string name)
        {
            Name = name;
        }

        public override void Fire()
        {
            Task.Run(() =>
            {
                ThemeManager.TimedSwitch(AutoDarkModeConfigBuilder.Instance().Config);
            });
        }
    }
}
