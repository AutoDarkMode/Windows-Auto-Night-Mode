namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class OfficeSwitchSettings
    {
        public Mode Mode { get; set; }
        public byte LightTheme { get; set; } = 0;
        public byte DarkTheme { get; set; } = 4;
    }
}
