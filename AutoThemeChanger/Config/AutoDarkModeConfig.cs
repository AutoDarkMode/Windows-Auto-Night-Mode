using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoThemeChanger.Config
{
    public class AutoDarkModeConfigBuilder
    {
        private static AutoDarkModeConfigBuilder instance;
        public AutoDarkModeConfig Config { get; set; }

        private const string FileName = "AutoDarkMode.json";
        protected AutoDarkModeConfigBuilder()
        {
            if (instance == null)
            {
                Config = new AutoDarkModeConfig();
            }
        }

        public static AutoDarkModeConfigBuilder GetInstance()
        {
            // not thread safe singleton pattern
            if (instance == null)
            {
                instance = new AutoDarkModeConfigBuilder();
            }
            return instance;
        }

        public void WriteConfig()
        {
            string jsonConfig = JsonConvert.SerializeObject(Config);
            using (StreamWriter file = new StreamWriter(Path.Combine(Environment.CurrentDirectory, FileName), false))
            {
                file.WriteLine(jsonConfig);
                file.Close();
            }
        }
    }

    public class AutoDarkModeConfig
    {
        public AutoDarkModeConfig()
        {
            Time = new Time()
            {
                IsLocationBased = false,
                SunRise = "07:00",
                SunRiseOffsetMin = 0,
                SunSet = "19:00",
                SunSetOffsetMin = 0,
            };
            Wallpaper = new Wallpaper
            {
                Disabled = true
            };
        }

        public bool Enabled { get; set; }

        private int appsTheme;
        public int AppsTheme
        {
            get { return appsTheme; }
            set
            {
                if (value >= 0 && value <= 2)
                {
                    appsTheme = value;
                }
            }
        }

        private int systemTheme;
        public int SystemTheme
        {
            get { return systemTheme; }
            set
            {
                if (value >= 0 && value <= 2)
                {
                    systemTheme = value;
                }
            }
        }

        public bool AccentColorTaskbar { get; set; }

        private int egdeTheme;
        public int EdgeTheme
        {
            get { return egdeTheme; }
            set
            {
                if (value >= 0 && value <= 3)
                {
                    egdeTheme = value;
                }
            }
        }
        public Wallpaper Wallpaper { get; set; }
        public Time Time { get; set; }

    }

    public class Wallpaper
    {
        public Wallpaper()
        {
            LightThemeWallpapers = new List<string>();
            DarkThemeWallpapers = new List<string>();
        }
        public bool Disabled { get; set; }
        public ICollection<string> LightThemeWallpapers { get; set; }
        public ICollection<string> DarkThemeWallpapers { get; set; }
    }

    public class Time
    {
        public bool IsLocationBased { get; set; }
        public string SunRise { get; set; }
        public string SunSet { get; set; }
        public int SunSetOffsetMin { get; set; }
        public int SunRiseOffsetMin { get; set; }
    }

}
