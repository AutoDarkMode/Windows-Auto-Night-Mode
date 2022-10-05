using System;
using System.Collections.Generic;
using System.ComponentModel;
using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings;
using AutoDarkModeConfig.ComponentSettings.Base;

namespace AutoDarkModeConfig
{
    public class AdmConfig
    {
        public AdmConfig()
        {
            Autostart = new();
            Location = new();
            Tunable = new();
            GPUMonitoring = new();
            Events = new();
            WindowsThemeMode = new();
            Updater = new();
            Hotkeys = new();
            IdleChecker = new();

            // New Component Settings;
            AppsSwitch = new();
            SystemSwitch = new();
            ColorFilterSwitch = new();
            OfficeSwitch = new();
            WallpaperSwitch = new();
        }
        public bool AutoThemeSwitchingEnabled { get; set; }
        public Autostart Autostart { get; set; }
        public WindowsThemeMode WindowsThemeMode { get; set; }
        public BaseSettingsEnabled<AppsSwitchSettings> AppsSwitch { get; set; }
        public BaseSettingsEnabled<SystemSwitchSettings> SystemSwitch { get; set; }
        public BaseSettings<object> ColorFilterSwitch { get; set; }
        public BaseSettings<OfficeSwitchSettings> OfficeSwitch { get; set; }
        public DateTime Sunrise { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 7, 0, 0);
        public DateTime Sunset { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 20, 0, 0);
        public Location Location { get; set; }
        public Tunable Tunable { get; set; }
        public GPUMonitoring GPUMonitoring { get; set; }
        public Events Events { get; set; }
        public Hotkeys Hotkeys { get; set; }
        public IdleChecker IdleChecker { get; set; }
        public BaseSettings<WallpaperSwitchSettings> WallpaperSwitch { get; set; }
        public Updater Updater { get; set; }
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
        public string ToggleAutoThemeSwitch { get; set; }
        public bool AutoThemeSwitchingNotification { get; set; } = true;
    }

    public class Addons
    {
        public Addons()
        {

        }
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
    }

    public class Events
    {
        public bool DarkThemeOnBattery { get; set; }
        public bool SystemResumeTrigger { get; set; } = true;
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
        public bool DebugTimerMessage { get; set; }
        public bool ShowTrayIcon { get; set; } = true;
        public string UICulture { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }

    public class GPUMonitoring
    {
        public bool Enabled { get; set; }
        public int Threshold { get; set; } = 30;
        private int monitorTimeSpanMin;
        public int MonitorTimeSpanMin
        {
            get { return monitorTimeSpanMin; }
            set
            {
                if (value <= 1)
                {
                    monitorTimeSpanMin = 1;
                }
                else
                {
                    monitorTimeSpanMin = value;
                }
            }
        }
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
}
