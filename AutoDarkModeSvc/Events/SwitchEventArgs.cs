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
using AutoDarkModeLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AutoDarkModeSvc.Events
{
    public class SwitchEventArgs : EventArgs
    {
        public SwitchEventArgs(SwitchSource source)
        {
            Source = source;
        }

        public SwitchEventArgs(SwitchSource source, Theme requestedTheme)
        {
            Source = source;
            Theme = requestedTheme;
        }

        public SwitchEventArgs(SwitchSource source, Theme requestedTheme, DateTime time, bool refreshDwm = false)
        {
            Source = source;
            Theme = requestedTheme;
            SwitchTime = time;
            RefreshDwm = refreshDwm;
        }

        public SwitchEventArgs(SwitchSource source, bool refreshDwm = false)
        {
            Source = source;
            RefreshDwm = refreshDwm;
        }   

        public void OverrideTheme(Theme newTheme, ThemeOverrideSource overrideSource)
        {
            if (Theme == Theme.Unknown)
            {
                Theme = newTheme;
            }
            else
            {
                Theme = newTheme;
                _themeOverrideSources.Add(overrideSource);
            }
        }

        /// <summary>
        /// Updates the switch time
        /// </summary>
        /// <param name="time">the switch time to set</param>
        public void UpdateSwitchTime(DateTime time)
        {
            SwitchTime = time;
        }

        public bool RefreshDwm { get; } = false;
        public SwitchSource Source { get; }
        private List<ThemeOverrideSource> _themeOverrideSources { get; } = new();
        public ReadOnlyCollection<ThemeOverrideSource> ThemeOverrideSources { get { return new(_themeOverrideSources); } }
        public Theme Theme { get; private set; } = Theme.Automatic;
        public DateTime? SwitchTime { get; private set; } = null;
    }
}