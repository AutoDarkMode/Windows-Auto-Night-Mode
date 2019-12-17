using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : IAutoDarkModeModule
    {
        public TimeSwitchModule(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool RunTask()
        {
            return true;
        }
    }
}
