using AutoDarkModeSvc.Interfaces;
using System;
using AutoDarkModeConfig;
using AutoDarkModeConfig.Interfaces;

namespace AutoDarkModeSvc.SwitchComponents
{
    abstract class BaseComponent<T> : ISwitchComponent
    {
        protected NLog.Logger Logger { get; private set; }
        protected ISwitchComponentSettings<T> Settings { get; set; }
        public bool Initialized { get; private set; }
        public BaseComponent()
        {
            Logger = NLog.LogManager.GetLogger(GetType().ToString());
        }
        public virtual int PriorityToLight { get; set; }
        public virtual int PriorityToDark { get; set; }
        public bool Enabled
        {
            get { return Settings.Enabled; }
        }
        public void Switch(Theme newTheme)
        {
            if (Enabled)
            {
                if (!Initialized)
                {
                    try
                    {
                        EnableHook();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"error while running enable hook");
                    }
                }
                if (ComponentNeedsUpdate(newTheme))
                {
                    HandleSwitch(newTheme);
                }
            }
            else if (Initialized)
            {
                try
                {
                    DisableHook();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"error while running disable hook");
                }
            }
        }
        public void UpdateSettingsState(object newSettings)
        {
            if (newSettings is ISwitchComponentSettings<T> temp)
            {
                Settings = temp;
            }
            else
            {
                Logger.Error($"could not convert generic settings object to ${typeof(T)}, no settings update performed.");
            }
        }
        public virtual void EnableHook()
        {
            Initialized = true;
        }
        public virtual void DisableHook()
        {
            Initialized = false;
        }
        /// <summary>
        /// True when the component should be compatible with the ThemeHandler switching mode
        /// </summary>
        public abstract bool ThemeHandlerCompatibility { get; }
        /// <summary>
        /// Entrypoint, called when a component needs to be updated
        /// </summary>
        /// <param name="newTheme">the new theme to apply</param>
        protected abstract void HandleSwitch(Theme newTheme);
        /// <summary>
        /// Determines whether the component needs to be triggered to update to the correct system state
        /// </summary>
        /// <returns>true if the component needs to be executed; false otherwise</returns>
        public abstract bool ComponentNeedsUpdate(Theme newTheme);
    }
}
