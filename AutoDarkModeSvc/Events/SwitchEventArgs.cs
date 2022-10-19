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

        public SwitchEventArgs(SwitchSource source, Theme requestedTheme, DateTime time)
        {
            Source = source;
            Theme = requestedTheme;
            Time = time;
        }

        public SwitchSource Source { get; }
        public Theme? Theme { get; } = null;
        public DateTime? Time { get; } = null;
    }
}