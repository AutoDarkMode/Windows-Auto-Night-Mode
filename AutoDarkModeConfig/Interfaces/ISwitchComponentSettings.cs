using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.Interfaces
{
    public interface ISwitchComponentSettings<T>
    {
        public bool Enabled { get; set; }
        public T Component { get; set; }
    }
}
