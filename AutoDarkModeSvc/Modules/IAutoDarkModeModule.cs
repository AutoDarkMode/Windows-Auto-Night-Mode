using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Modules
{
    interface IAutoDarkModeModule
    {
        public void Poll();

        public string Name { get; }
    }
}
