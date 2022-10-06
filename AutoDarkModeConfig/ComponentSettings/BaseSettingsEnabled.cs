using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeLib.ComponentSettings
{
    public class BaseSettingsEnabled<T> : BaseSettings<T>
    {
        public override bool Enabled { get; set; } = true;
    }
}
