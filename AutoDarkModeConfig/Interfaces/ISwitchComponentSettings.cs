using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeLib.Interfaces
{
    public interface ISwitchComponentSettings<T>
    {
        public bool Enabled { get; set; }
        public T Component { get; set; }
    }
}
