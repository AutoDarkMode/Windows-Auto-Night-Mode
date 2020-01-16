using System;
using System.Collections.Generic;

namespace AutoDarkModeApp.Config
{
    public class AutoDarkModeConfig
    {
        public AutoDarkModeConfig()
        {
            Wallpaper = new Wallpaper();
            Location = new Location();
        }

        private int appsTheme;
        private int systemTheme;
        private int egdeTheme;

        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public bool Enabled { get; set; }
        public bool AccentColorTaskbar { get; set; }
        public bool ClassicMode { get; set; }
        public int AppsTheme
        {
            get { return appsTheme; }
            set
            {
                if (value >= 0 && value <= 2)
                {
                    appsTheme = value;
                }
                else
                {
                    // DEFAULT
                    appsTheme = 0;
                }
            }
        }
        public int SystemTheme
        {
            get { return systemTheme; }
            set
            {
                if (value >= 0 && value <= 2)
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
        public int EdgeTheme
        {
            get { return egdeTheme; }
            set
            {
                if (value >= 0 && value <= 3)
                {
                    egdeTheme = value;
                }
                else
                {
                    // DEFAULT
                    egdeTheme = 3;
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
