using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    class ThemeModeEvent : ConfigUpdateEvent<AdmConfig>
    {
        private readonly ComponentManager cm;
        public ThemeModeEvent(ComponentManager cm) {
            this.cm = cm;
        }
        protected override void ChangeEvent()
        {
            bool themeModeToggled = newConfig.WindowsThemeMode.Enabled != oldConfig.WindowsThemeMode.Enabled;
            // if the theme mode is toggled to off, we need to reinitialize all components
            if (themeModeToggled)
            {
                if (newConfig.WindowsThemeMode.Enabled) {
                    cm.InvokeDisableIncompatible();
                    // currently unused due to change of how active theme monitoring is processed
                    // if (newConfig.WindowsThemeMode.MonitorActiveTheme) WindowsThemeMonitor.StartThemeMonitor();
                }
                else
                {
                    // currently unused due to change of how active theme monitoring is processed

                    //WindowsThemeMonitor.StopThemeMonitor();
                }
            } 
            else if (newConfig.WindowsThemeMode.Enabled)
            {
                // currently unused due to change of how active theme monitoring is processed
                /*
                bool monitorThemeToggled = newConfig.WindowsThemeMode.MonitorActiveTheme != oldConfig.WindowsThemeMode.MonitorActiveTheme;
                if (monitorThemeToggled)
                {
                    if (newConfig.WindowsThemeMode.MonitorActiveTheme) WindowsThemeMonitor.StartThemeMonitor();
                    else WindowsThemeMonitor.StopThemeMonitor();
                }
                */
            }
        }
    }
}
