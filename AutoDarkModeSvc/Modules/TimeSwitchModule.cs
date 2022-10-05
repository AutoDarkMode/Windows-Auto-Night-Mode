using System;
using AutoDarkModeConfig;
using System.Threading.Tasks;
using AutoDarkModeSvc.Timers;
using System.Diagnostics.CodeAnalysis;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.Main;
        private AdmConfigBuilder ConfigBuilder { get; }
        private GlobalState State { get; } = GlobalState.Instance();

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public TimeSwitchModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AdmConfigBuilder.Instance();
        }

        public override void Fire()
        {
            if (!State.PostponeManager.IsPostponed)
            {
                Task.Run(() =>
                {
                    ThemeManager.RequestSwitch(new(SwitchSource.TimeSwitchModule));
                });
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }
    }
}
