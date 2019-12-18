using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeApp.Config;

namespace AutoDarkModeSvc.Modules
{
    class ConfigRefreshModule : IAutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string Name { get; }

        public ConfigRefreshModule(string name)
        {
            Name = name;
        }

        public void Poll(AutoDarkModeConfig Config)
        {
            return;
        }

        public void Poll()
        {
            AutoDarkModeConfigBuilder Builder = AutoDarkModeConfigBuilder.Instance();
            Task.Run(() =>
            {
                try
                {
                    Builder.Read();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not read config file");
                }
            });
        }
    }
}
