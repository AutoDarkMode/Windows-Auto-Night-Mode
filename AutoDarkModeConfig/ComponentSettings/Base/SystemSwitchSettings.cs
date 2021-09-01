using AutoDarkModeConfig.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class SystemSwitchSettings
    {
        public Mode Mode { get; set; }
        public int TaskDelay { get; set; }
        public bool ToggleTaskbarColor { get; set; }
    }
}
