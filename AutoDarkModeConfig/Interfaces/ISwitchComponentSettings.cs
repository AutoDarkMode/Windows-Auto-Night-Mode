using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeConfig.Interfaces
{
    public interface ISwitchComponentSettings<T>
    {
        public int PriorityToLight { get; set; }
        public int PriorityToDark { get; set; }
        public bool Enabled { get; set; }
        public T Component { get; set; }
    }
}
