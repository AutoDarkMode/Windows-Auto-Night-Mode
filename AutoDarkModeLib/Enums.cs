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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeLib
{
    public enum Mode
    {
        Switch = 0,
        LightOnly = 1,
        DarkOnly = 2,
        AccentOnly = 3,
        FollowSystemTheme = 4
    };
    public enum Theme
    {
        Ignore = -2,
        Unknown = -1,
        Dark = 0,
        Light = 1,
        Automatic = 2
    };

    /// <summary>
    /// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
    /// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
    /// </summary>
    public enum WallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    public enum SwitchSource
    {
        Any,
        TimeSwitchModule,
        NightLightTrackerModule,
        BatteryStatusChanged,
        SystemResume,
        Manual,
        ExternalThemeSwitch,
        Startup,
        SystemUnlock,
        Api,
        SystemTimeChanged
    }

    public enum ThemeOverrideSource
    {
        Default = 0,
        TimedThemeState,
        NightLight,
        ForceFlag,
        BatteryStatus,
        PostponeManager
    }

    public enum Governor
    {
        Default,
        NightLight
    }

    public enum SkipType
    {
        Unspecified,
        UntilSunset,
        UntilSunrise
    }

    public enum HookPosition
    {
        PreSync,
        PostSync
    }

    public enum DwmRefreshType
    {
        None = 0,
        Standard = 1,
        Full = 2
    }

    public enum BridgeResponseCode
    {
        InvalidArguments,
        Success,
        Fail,
        NotFound
    }

    public enum WindowsBuilds : int
    {
        MinBuildForNewFeatures = 19044,
        Win11_RC = 22000,
        Win11_22H2 = 22621,
    }

    public enum WindowsBuildsUbr : int
    {
        Win11_22H2_Spotlight = 1105
    }
}
