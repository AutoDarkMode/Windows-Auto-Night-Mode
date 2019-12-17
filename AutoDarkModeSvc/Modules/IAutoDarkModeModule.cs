using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    interface IAutoDarkModeModule
    {
        public bool RunTask();

        public string Name { get; }
    }
}
