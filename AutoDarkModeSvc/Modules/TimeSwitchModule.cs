using System;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : IAutoDarkModeModule
    {
        public TimeSwitchModule(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public void Poll(AutoDarkModeConfig config)
        {
            Task.Run(() =>
            {
                ThemeManager.TimedSwitch(config);
            });
        }

        public void Poll()
        {
            return;
        }
    }
}
