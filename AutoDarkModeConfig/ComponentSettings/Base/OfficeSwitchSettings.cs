namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class OfficeSwitchSettings
    {
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
                    mode = Mode.Switch;
                }
            }
        }

        public byte LightTheme { get; set; } = 0;
        public byte DarkTheme { get; set; } = 4;
    }
}
