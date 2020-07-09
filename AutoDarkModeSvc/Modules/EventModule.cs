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
        private readonly AdmConfigBuilder builder;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool DarkThemeOnBatteryEnabled;
        private bool ResumeEventEnabled;
        public EventModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            builder = AdmConfigBuilder.Instance();
        }
        public override void Fire()
        {
            if (builder.Config.Events.DarkThemeOnBattery) {
                if (!DarkThemeOnBatteryEnabled)
                {
                    PowerEventHandler.RegisterThemeEvent();
                    Logger.Info("enabling event handler for dark mode on battery state discharging");
                    DarkThemeOnBatteryEnabled = true;
                }
            }
            else
            {
                if (DarkThemeOnBatteryEnabled)
                {
                    PowerEventHandler.DeregisterThemeEvent();
                    Logger.Info("disabling event handler for dark mode on battery state discharging");
                    DarkThemeOnBatteryEnabled = false;
                }
            }

            if (builder.Config.Tunable.SystemResumeTrigger)
            {
                if (!ResumeEventEnabled)
                {
                    PowerEventHandler.RegisterResumeEvent();
                    Logger.Info("enabling theme refresh at system resume");
                    ResumeEventEnabled = true;
                }

            }
            else
            {
                if (ResumeEventEnabled)
                {
                    PowerEventHandler.DeregisterResumeEvent();
                    Logger.Info("disabling theme refresh at system resume");
                    ResumeEventEnabled = false;
                }
            }
        }
    }
}
