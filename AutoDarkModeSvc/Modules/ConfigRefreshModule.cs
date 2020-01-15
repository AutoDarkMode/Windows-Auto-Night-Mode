using System;
using System.Threading.Tasks;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules
{
    class ConfigRefreshModule : IAutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private AutoDarkModeConfigBuilder ConfigBuilder { get;  }

        public string Name { get; }

        public string TimerAffinity { get; } = TimerName.IO;

        public ConfigRefreshModule(string name)
        {
            Name = name;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }
        public void Poll()
        {

            Task.Run(() =>
            {
                try
                {
                    ConfigBuilder.Read();
                    Logger.Debug("updated configuration file");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not read config file");
                }
            });
        }
    }
}
