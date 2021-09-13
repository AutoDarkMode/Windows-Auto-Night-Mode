using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class WallpaperSwitchSettings
    {
        public WallpaperSwitchSettings()
        {
            Monitors = new();
        }
        private Mode mode;
        //[JsonConverter(typeof(StringEnumConverter))]
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
        public WallpaperPosition Position { get; set; } = WallpaperPosition.Fit;
        public List<MonitorSettings> Monitors { get; set; }
    }

    public class MonitorSettings
    {
        public string Id { get; set; }
        public string LightThemeWallpaper { get; set; }
        public string DarkThemeWallpaper { get; set; }
    }
}
