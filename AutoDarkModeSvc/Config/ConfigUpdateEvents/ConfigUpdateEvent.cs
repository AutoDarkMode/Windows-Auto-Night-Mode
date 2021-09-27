using AutoDarkModeConfig;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config.ConfigUpdateEvents
{
    public abstract class ConfigUpdateEvent<T> : IConfigUpdateEvent<T>
    {
        protected NLog.Logger Logger { get; private set; }
        protected T oldConfig;
        protected T newConfig;
        public ConfigUpdateEvent()
        {
            Logger = NLog.LogManager.GetLogger(GetType().ToString());
        }
        public void OnConfigUpdate(object sender, T newConfig)
        {
            if (sender is T oldConfig)
            {
                this.oldConfig = oldConfig;
                this.newConfig = newConfig;
                ChangeEvent();
            }
        }
        protected abstract void ChangeEvent();
    }
}
