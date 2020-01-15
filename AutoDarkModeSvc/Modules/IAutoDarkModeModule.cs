using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeApp.Config;

namespace AutoDarkModeSvc.Modules
{
    interface IAutoDarkModeModule
    {
        public void Poll();
        public string Name { get; }
        public string TimerAffinity { get;  }
    }
}
