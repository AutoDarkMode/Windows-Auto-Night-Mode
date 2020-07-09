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
        private readonly RuntimeConfig rtc;
        public EventModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            builder = AdmConfigBuilder.Instance();
            rtc = RuntimeConfig.Instance();
        }
        public override void Fire()
        {
            if (builder.Config.Events.DarkThemeOnBattery) {
                if (!rtc.DarkThemeOnBattery)
                {
                    PowerEventHandler.RegisterThemeEvent();
                    Logger.Info("enabling event handler for dark mode on battery state discharging");
                    rtc.DarkThemeOnBattery = true;
                }
            }
            else
            {
                if (rtc.DarkThemeOnBattery)
                {
                    PowerEventHandler.DeregisterThemeEvent();
                    Logger.Info("disabling event handler for dark mode on battery state discharging");
                    rtc.DarkThemeOnBattery = false;
                }
            }

            if (builder.Config.Tunable.SystemResumeTrigger)
            {
                if (!rtc.ResumeEvent)
                {
                    PowerEventHandler.RegisterResumeEvent();
                    Logger.Info("enabling theme refresh at system resume");
                    rtc.ResumeEvent = true;
                }

            }
            else
            {
                if (rtc.ResumeEvent)
                {
                    PowerEventHandler.DeregisterResumeEvent();
                    Logger.Info("disabling theme refresh at system resume");
                    rtc.ResumeEvent = false;
                }
            }
        }
    }
}
