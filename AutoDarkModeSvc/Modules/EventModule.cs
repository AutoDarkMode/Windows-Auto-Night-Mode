using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    class EventModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.Main;
        public readonly AdmConfigBuilder builder;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public EventModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            builder = AdmConfigBuilder.Instance();
        }
        public override void Fire()
        {
            if (builder.Config.Events.DarkThemeOnBattery) {
                PowerManagerEventHandler.RegisterThemeEvent();
                Logger.Info("enabling event handler for dark mode on battery state discharging");
            }
            else
            {
                PowerManagerEventHandler.DeregisterThemeEvent();
                Logger.Info("disabling event handler for dark mode on battery state discharging");
            }
        }
    }
}
