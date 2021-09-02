using AutoDarkModeConfig.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class AppsSwitchSettings
    {
        Mode mode;
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
    }
}
