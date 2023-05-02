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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeLib.IThemeManager2
{
    public static class Flags
    {
        [Flags]
        public enum InitializationFlags
        {
            ThemeInitNoFlags = 0,
            ThemeInitCurrentThemeOnly = 1 << 0,
            ThemeInitFlagUnk1 = 1 << 1,
            ThemeInitFlagUnk2 = 1 << 2,
        };

        [Flags]
        public enum ThemeApplyFlags
        {
            IgnoreBackground = 1 << 0,
            IgnoreCursor = 1 << 1,
            IgnoreDesktopIcons = 1 << 2,
            IgnoreColor = 1 << 3,
            IgnoreSound = 1 << 4,
            IgnoreScreensaver = 1 << 5,
            Unknown = 1 << 6, // something about window metrics
            Unknown2 = 1 << 7,
            NoHourglass = 1 << 8
        };

        [Flags]
        public enum ThemePackFlags
        {
            Unknown1 = 1 << 0, // setting this seems to supress hourglass
            Unknown2 = 1 << 1, // setting this seems to supress hourglass
            Silent = 1 << 2, // hides all dialogs and prevents sound
            Roamed = 1 << 3, // something about roaming
        };

        public enum DesktopWallpaperPosition
        {
        }

        public enum ThemeCategory
        {
        }
    }
}
