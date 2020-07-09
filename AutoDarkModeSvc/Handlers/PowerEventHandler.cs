using AutoDarkModeSvc.Config;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.System.Power;

namespace AutoDarkModeSvc.Handlers
{
    class PowerEventHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void RegisterThemeEvent()
        {
            PowerManager.BatteryStatusChanged += PowerManager_BatteryStatusChanged;
        }

        private static void PowerManager_BatteryStatusChanged(object sender, object e)
        {
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            if (PowerManager.BatteryStatus == BatteryStatus.Discharging) {
                Logger.Info("battery discharging, enabling dark mode");
                ThemeManager.SwitchTheme(builder.Config, Theme.Dark, false);
            }
            else
            {
                ThemeManager.TimedSwitch(builder);
            }
        }

        public static void DeregisterThemeEvent()
        {
            PowerManager.BatteryStatusChanged -= PowerManager_BatteryStatusChanged;
            ThemeManager.TimedSwitch(AdmConfigBuilder.Instance());
        }

        public static void RegisterResumeEvent()
        {
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Logger.Info("system resuming from suspended state, refreshing theme");
                ThemeManager.TimedSwitch(AdmConfigBuilder.Instance());
            }
        }

        public static void DeregisterResumeEvent()
        {
            
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }
    }
}
