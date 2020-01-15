using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutoDarkModeApp.Config
{
    public class AutoDarkModeConfigBuilder
    {
        private static AutoDarkModeConfigBuilder instance;
        public AutoDarkModeConfig Config { get; private set; }

        private const string FileName = "AutoDarkModeConfig.json";
        protected AutoDarkModeConfigBuilder()
        {
            if (instance == null)
            {
                Config = new AutoDarkModeConfig();
            }
        }

        public static AutoDarkModeConfigBuilder Instance()
        {
            if (instance == null)
            {
                instance = new AutoDarkModeConfigBuilder();
            }
            return instance;
        }

        public void Save()
        {
            try
            {
                string jsonConfig = JsonConvert.SerializeObject(Config, Formatting.Indented);
                using StreamWriter writer = new StreamWriter(Path.Combine(Environment.CurrentDirectory, FileName), false);
                writer.WriteLine(jsonConfig);
                writer.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Load()
        {
            string path = Path.Combine(Environment.CurrentDirectory, FileName);
            if (!File.Exists(path))
            {
                Save();
            }
            try
            {
                using StreamReader reader = File.OpenText(path);
                JsonSerializer serializer = new JsonSerializer();
                Config = (AutoDarkModeConfig)serializer.Deserialize(reader, typeof(AutoDarkModeConfig));
                reader.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

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
        public bool Disabled { get; set; }
        public ICollection<string> LightThemeWallpapers { get; set; }
        public ICollection<string> DarkThemeWallpapers { get; set; }
    }

    public class Location
    {
        public bool Disabled { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int SunsetOffsetMin { get; set; }
        public int SunriseOffsetMin { get; set; }
    }
}
