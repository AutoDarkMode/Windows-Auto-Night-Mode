using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeLib.ComponentSettings.Base
{
    public class ColorizationSwitchSettings
    {
        public string LightHex { get; set; } = "";
        public string DarkHex { get; set; } = "";
        public bool LightAutoColorization { get; set; }
        public bool DarkAutoColorization { get; set; }
    }
}
