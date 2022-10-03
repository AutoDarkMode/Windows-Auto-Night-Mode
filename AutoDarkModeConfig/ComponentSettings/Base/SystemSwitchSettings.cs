namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class SystemSwitchSettings
    {
        public Mode Mode { get; set; }
        public int TaskbarSwitchDelay { get; set; } = 1200;
        public bool TaskbarColorOnAdaptive { get; set; }
        public Theme TaskbarColorWhenNonAdaptive { get; set; } = Theme.Light;
        public bool DWMPrevalenceSwitch { get; set; }
        public Theme DWMPrevalenceEnableTheme { get; set; } = Theme.Light;
    }
}
