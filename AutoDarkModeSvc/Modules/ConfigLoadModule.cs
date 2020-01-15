using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules
{
    class ConfigLoadModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private AutoDarkModeConfigBuilder ConfigBuilder { get;  }


        /// <summary>
        /// Instantiates a new ConfigUpdateModule.
        /// This module reloads the configuration file periodically
        /// </summary>
        /// <param name="name">unique name of the module</param>
        /// <param name="timerAffinity">name of the timer this module should be assigned to</param>
        public ConfigLoadModule(string name, string timerAffinity)
        {
            Name = name;
            TimerAffinity = timerAffinity;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }
        public override void Poll()
        {
            Task.Run(() =>
            {
                try
                {
                    ConfigBuilder.Load();
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
