using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules
{
    [Obsolete]
    class ConfigLoadModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private AutoDarkModeConfigBuilder ConfigBuilder { get;  }
        public override string TimerAffinity { get; } = TimerName.IO;
         
        /// <summary>
        /// Instantiates a new ConfigUpdateModule.
        /// This module reloads the configuration file periodically
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public ConfigLoadModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }
        public override void Fire()
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
