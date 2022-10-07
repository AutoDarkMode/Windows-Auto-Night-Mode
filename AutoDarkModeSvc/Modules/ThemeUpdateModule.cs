using AutoDarkModeLib;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.Modules
{
    class ThemeUpdateModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public override string TimerAffinity { get; } = TimerName.StateUpdate;
        private GlobalState State { get; }

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public ThemeUpdateModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            State = GlobalState.Instance();
        }

        public override void Fire()
        {
            Thread thread = new(() =>
            {
                try
                {
                    State.CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"couldn't refresh currently active theme");
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public override void DisableHook()
        {
            State.CurrentWindowsThemeName = "";
        }
    }
}
