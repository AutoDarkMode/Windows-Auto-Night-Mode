using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class WallpaperSwitchSettings
    {
        public WallpaperSwitchSettings()
        {
            Monitors = new();
        }
        private Mode mode;
        [JsonConverter(typeof(StringEnumConverter))]
        public Mode Mode
        {
            get { return mode; }
            set
            {
                if (value >= 0 && (int)value <= 2)
                {
                    mode = value;
                }
                else
                {
                    // DEFAULT
                    mode = 0;
                }
            }
        }
        public List<MonitorSettings> Monitors { get; set; }
    }

    public class MonitorSettings
    {
        public string Id { get; set; }
        public string LightThemeWallpaper { get; set; }
        public string DarkThemeWallpaper { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public WallpaperPosition Position { get; set; } = WallpaperPosition.Fill;
    }
}
