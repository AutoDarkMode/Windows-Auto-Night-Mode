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
using System.ComponentModel;
using AutoDarkModeLib.ComponentSettings;
using AutoDarkModeLib.ComponentSettings.Base;
using static AutoDarkModeLib.IThemeManager2.Flags;

namespace AutoDarkModeLib.Configs
{
    public class AdmConfig
    {
        public AdmConfig()
        {
            Autostart = new();
            Location = new();
            Tunable = new();
            GPUMonitoring = new();
            ProcessBlockList = new();
            Events = new();
            WindowsThemeMode = new();
            Updater = new();
            Hotkeys = new();
            IdleChecker = new();
            Notifications = new();
            AutoSwitchNotify = new();

            // New Component Settings;
            AppsSwitch = new();
            SystemSwitch = new();
            TouchKeyboardSwitch = new();
            ColorFilterSwitch = new();
            OfficeSwitch = new();
            WallpaperSwitch = new();
            ColorizationSwitch = new();
            CursorSwitch = new();
        }
        public bool AutoThemeSwitchingEnabled { get; set; }
        public Governor Governor { get; set; } = Governor.Default;
        public Autostart Autostart { get; set; }
        public WindowsThemeMode WindowsThemeMode { get; set; }
        public BaseSettingsEnabled<AppsSwitchSettings> AppsSwitch { get; set; }
        public BaseSettingsEnabled<SystemSwitchSettings> SystemSwitch { get; set; }
        public BaseSettings<object> TouchKeyboardSwitch { get; set; }
        public BaseSettings<ColorizationSwitchSettings> ColorizationSwitch { get; set; }
        public BaseSettings<object> ColorFilterSwitch { get; set; }
        public BaseSettings<OfficeSwitchSettings> OfficeSwitch { get; set; }
        public BaseSettings<CursorSwitchSettings> CursorSwitch { get; set; }
        public DateTime Sunrise { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 7, 0, 0);
        public DateTime Sunset { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 20, 0, 0);
        public Location Location { get; set; }
        public Tunable Tunable { get; set; }
        public GPUMonitoring GPUMonitoring { get; set; }
        public ProcessBlockList ProcessBlockList { get; set; }
        public Events Events { get; set; }
        public Notifications Notifications { get; set; }
        public AutoSwitchNotify AutoSwitchNotify { get; set; }
        public Hotkeys Hotkeys { get; set; }
        public IdleChecker IdleChecker { get; set; }
        public BaseSettings<WallpaperSwitchSettings> WallpaperSwitch { get; set; }
        public Updater Updater { get; set; }
    }

    public class AutoSwitchNotify
    {
        public bool Enabled { get; set; } = false;
        public int GracePeriodMinutes { get; set; } = 5;
    }

    public class Notifications
    {
        public bool OnAutoThemeSwitching { get; set; } = true;
        public bool OnSkipNextSwitch { get; set; } = true;
    }

    public class IdleChecker
    {
        public bool Enabled { get; set; }
        public int Threshold { get; set; } = 5;
    }

    public class Hotkeys
    {
        public bool Enabled { get; set; }
        public string ForceLight { get; set; }
        public string ForceDark { get; set; }
        public string NoForce { get; set; }
        public string ToggleTheme { get; set; }
        public string TogglePostpone { get; set; }
        public string ToggleAutoThemeSwitch { get; set; }
    }

    public class Addons
    {
        //put your custom settings here!
    }

    public class Autostart
    {
        public bool Validate { get; set; } = true;
    }

    public class Updater
    {
        public bool AutoInstall { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Silent { get; set; }
        public int DaysBetweenUpdateCheck { get; set; } = 7;
        public bool CheckOnStart { get; set; }
        public string VersionQueryUrl { get; set; }
        public string DownloadBaseUrl { get; set; }
        public string ZipCustomUrl { get; set; }
        public string HashCustomUrl { get; set; }
    }

    public class WindowsThemeMode
    {
        public bool Enabled { get; set; }
        public string DarkThemePath { get; set; }
        public string LightThemePath { get; set; }
        public bool MonitorActiveTheme { get; set; }
        public List<ThemeApplyFlags> ApplyFlags { get; set; } = new();
    }

    public class Events
    {
        public bool DarkThemeOnBattery { get; set; }
        public bool Win10AllowLockscreenSwitch { get; set; } = false;
    }

    public class Location
    {
        public TimeSpan PollingCooldownTimeSpan { get; set; } = TimeSpan.FromHours(24);
        public bool Enabled { get; set; }
        public bool UseGeolocatorService { get; set; } = true;
        public int SunsetOffsetMin { get; set; }
        public int SunriseOffsetMin { get; set; }
        public double CustomLat { get; set; }
        public double CustomLon { get; set; }
    }

    public class Tunable
    {
        private int batterySliderDefaultValue = 25;
        public int BatterySliderDefaultValue
        {
            get { return batterySliderDefaultValue; }
            set
            {
                if (value < 0)
                {
                    batterySliderDefaultValue = 0;
                }
                else if (value > 100)
                {
                    batterySliderDefaultValue = 100;
                }
                else
                {
                    batterySliderDefaultValue = value;
                }
            }
        }
        public bool DisableEnergySaverOnThemeSwitch { get; set; }
        public bool UseLogonTask { get; set; }
        public bool Debug { get; set; }
        public bool Trace { get; set; }
        public bool DebugTimerMessage { get; set; }
        public bool ShowTrayIcon { get; set; } = true;
        public bool AlwaysFullDwmRefresh { get; set; } = false;
        public string UICulture { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }

    public class GPUMonitoring
    {
        public bool Enabled { get; set; }
        public int Threshold { get; set; } = 30;
        private int samples = 1;
        public int Samples
        {
            get { return samples; }
            set
            {
                if (value < 1)
                {
                    samples = 1;
                }
                else
                {
                    samples = value;
                }
            }
        }
    }

    /// <summary>
    /// Configures the <see cref="AutoDarkModeSvc.Modules.BlockListModule"/>, used for postponing theme switches while
    /// some processes are running
    /// </summary>
    public class ProcessBlockList
    {
        public SortedSet<string> ProcessNames { get; set; } = new();
        public bool Enabled { get; set; }
    }
}
