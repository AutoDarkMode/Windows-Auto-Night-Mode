using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config.ConfigUpdateEvents
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
                    if (newConfig.WindowsThemeMode.MonitorActiveTheme) GlobalState.Instance().StartThemeMonitor();
                }
                else
                {
                    GlobalState.Instance().StopThemeMonitor();
                }
            } 
            else if (newConfig.WindowsThemeMode.Enabled)
            {
                bool monitorThemeToggled = newConfig.WindowsThemeMode.MonitorActiveTheme != oldConfig.WindowsThemeMode.MonitorActiveTheme;
                if (monitorThemeToggled)
                {
                    if (newConfig.WindowsThemeMode.MonitorActiveTheme) GlobalState.Instance().StartThemeMonitor();
                    else GlobalState.Instance().StopThemeMonitor();
                }
            }
        }
    }
}
