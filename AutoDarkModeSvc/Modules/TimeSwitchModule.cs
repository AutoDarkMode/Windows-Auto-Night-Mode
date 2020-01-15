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
        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        /// <param name="timerAffinity">name of the timer this module should be assigned to</param>
        public TimeSwitchModule(string name, string timerAffinity)
        {
            TimerAffinity = timerAffinity;
            Name = name;
        }

        public override void Poll()
        {
            Task.Run(() =>
            {
                ThemeManager.TimedSwitch(AutoDarkModeConfigBuilder.Instance().Config);
            });
        }
    }
}
