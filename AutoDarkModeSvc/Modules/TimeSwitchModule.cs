using System;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : IAutoDarkModeModule
    {
        public string Name { get; }
        public TimeSwitchModule(string name)
        {
            Name = name;
        }
        public void Poll()
        {
            Task.Run(() =>
            {
                ThemeManager.TimedSwitch(AutoDarkModeConfigBuilder.Instance().Config);
            });
        }
    }
}
