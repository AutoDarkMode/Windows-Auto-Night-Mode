using System;
using AutoDarkModeApp.Config;
using AutoDarkModeApp;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : IAutoDarkModeModule
    {
        public string Name { get; }

        public string TimerAffinity { get; } = TimerName.Main;

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
