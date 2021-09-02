using AutoDarkModeConfig.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.ComponentSettings.Base
{
    public class AppsSwitchSettings
    {
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
                    mode = Mode.Switch;
                }
            }
        }
    }
}
