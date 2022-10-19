// Copyright (c) 2022 namazso <admin@namazso.eu>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
