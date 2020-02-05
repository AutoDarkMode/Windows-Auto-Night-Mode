using System;
using System.Collections.Generic;

namespace AutoDarkModeSvc.Config
{
    public class AutoDarkModeConfig
    {
        public AutoDarkModeConfig()
        {
            Wallpaper = new Wallpaper();
            Location = new Location();
        }

        private Mode appsTheme;
        private Mode systemTheme;
        private Mode egdeTheme;

        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public bool AutoThemeSwitchingEnabled { get; set; }
        public bool AccentColorTaskbarEnabled { get; set; }
        public bool ClassicMode { get; set; }
        public Mode AppsTheme
        {
            get { return appsTheme; }
            set
            {
                if ((int)value >= 0 && (int)value <= 2)
                {
                    appsTheme = value;
                }
                else
                {
                    // DEFAULT
                    appsTheme = Mode.Switch;
                }
            }
        }
        public Mode SystemTheme
        {
            get { return systemTheme; }
            set
            {
                if ((int)value >= 0 && (int)value <= 2)
                {
                    systemTheme = value;
                }
                else
                {
                    // DEFAULT
                    systemTheme = 0;
                }
            }
        }
        public Mode EdgeTheme
        {
            get { return egdeTheme; }
            set
            {
                if ((int)value >= 0 && (int)value <= 3)
                {
                    egdeTheme = value;
                }
                else
                {
                    // DEFAULT
                    egdeTheme = Mode.Switch;
                }
            }
        }
        public Wallpaper Wallpaper { get; set; }
        public Location Location { get; set; }
    }

    public class Wallpaper
    {
        public Wallpaper()
        {
            LightThemeWallpapers = new List<string>();
            DarkThemeWallpapers = new List<string>();
        }
        public bool Enabled { get; set; }
        public ICollection<string> LightThemeWallpapers { get; set; }
        public ICollection<string> DarkThemeWallpapers { get; set; }
    }

    public class Location
    {
        public bool Enabled { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int SunsetOffsetMin { get; set; }
        public int SunriseOffsetMin { get; set; }
    }
}
