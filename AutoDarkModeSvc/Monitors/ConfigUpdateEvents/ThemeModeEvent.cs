#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib.Configs;
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
        private readonly GlobalState state = GlobalState.Instance();
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
                    if (newConfig.WindowsThemeMode.MonitorActiveTheme) WindowsThemeMonitor.StartThemeMonitor();
                    state.RefreshThemes(newConfig);
                }
                else
                {
                    WindowsThemeMonitor.StopThemeMonitor();
                }
            } 
            else if (newConfig.WindowsThemeMode.Enabled)
            {

                bool darkThemeChanged = newConfig.WindowsThemeMode.DarkThemePath != oldConfig.WindowsThemeMode.DarkThemePath;
                bool lightThemeChanged = newConfig.WindowsThemeMode.LightThemePath != oldConfig.WindowsThemeMode.LightThemePath;
                bool flagsChanged = newConfig.WindowsThemeMode.ApplyFlags.SequenceEqual(oldConfig.WindowsThemeMode.ApplyFlags);
                if (darkThemeChanged || lightThemeChanged || flagsChanged)
                {
                    state.RefreshThemes(newConfig);
                }


                bool monitorThemeToggled = newConfig.WindowsThemeMode.MonitorActiveTheme != oldConfig.WindowsThemeMode.MonitorActiveTheme;
                if (monitorThemeToggled)
                {
                    if (newConfig.WindowsThemeMode.MonitorActiveTheme) WindowsThemeMonitor.StartThemeMonitor();
                    else WindowsThemeMonitor.StopThemeMonitor();
                }
            }
        }
    }
}
