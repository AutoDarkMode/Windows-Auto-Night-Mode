using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.IThemeManager2
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
            ThemeApplyFlagIgnoreBackground = 1 << 0,
            ThemeApplyFlagIgnoreCursor = 1 << 1,
            ThemeApplyFlagIgnoreDesktopIcons = 1 << 2,
            ThemeApplyFlagIgnoreColor = 1 << 3,
            ThemeApplyFlagIgnoreSound = 1 << 4,
            ThemeApplyFlagIgnoreScreensaver = 1 << 5,
            ThemeApplyFlagUnknown = 1 << 6, // something about window metrics
            ThemeApplyFlagUnknown2 = 1 << 7,
            ThemeApplyFlagNoHourglass = 1 << 8
        };

        [Flags]
        public enum ThemePackFlags
        {
            ThemepackFlagUnknown1 = 1 << 0, // setting this seems to supress hourglass
            ThemepackFlagUnknown2 = 1 << 1, // setting this seems to supress hourglass
            ThemepackFlagSilent = 1 << 2, // hides all dialogs and prevents sound
            ThemepackFlagRoamed = 1 << 3, // something about roaming
        };

        public enum DesktopWallpaperPosition
        {
        }

        public enum ThemeCategory
        {
        }
    }
}
