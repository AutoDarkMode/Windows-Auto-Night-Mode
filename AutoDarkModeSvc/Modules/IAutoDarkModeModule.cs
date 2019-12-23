using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeApp.Config;

namespace AutoDarkModeSvc.Modules
{
    interface IAutoDarkModeModule
    {
        public void Poll(AutoDarkModeConfig Config);
        public void Poll();
        public string Name { get; }
    }
}
