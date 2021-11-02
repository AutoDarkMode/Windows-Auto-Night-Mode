namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class SystemSwitchSettings
    {
        private Mode mode;
        //[JsonConverter(typeof(StringEnumConverter))]
        public Mode Mode
        {
            get => mode;
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
        public int TaskbarSwitchDelay { get; set; } = 1200;
        public bool TaskbarColorOnAdaptive { get; set; }
        public Theme TaskbarColorWhenNonAdaptive { get; set; } = Theme.Light;
    }
}
