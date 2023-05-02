#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeSvc.Interfaces;
using System;
using AutoDarkModeLib;
using AutoDarkModeLib.Interfaces;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Core;
using Windows.ApplicationModel.VoiceCommands;

namespace AutoDarkModeSvc.SwitchComponents
{
    abstract class BaseComponent<T> : ISwitchComponent
    {
        protected NLog.Logger Logger { get; private set; }
        protected GlobalState GlobalState { get; } = GlobalState.Instance();
        protected ISwitchComponentSettings<T> Settings { get; set; }
        protected ISwitchComponentSettings<T> SettingsBefore { get; set; }
        public bool Initialized { get; private set; }
        public BaseComponent()
        {
            Logger = NLog.LogManager.GetLogger(GetType().ToString());
        }
        public virtual DwmRefreshType TriggersDwmRefresh { get; protected set; } = DwmRefreshType.None;
        public virtual DwmRefreshType NeedsDwmRefresh { get; protected set; } = DwmRefreshType.None;
        public virtual int PriorityToLight { get; }
        public virtual int PriorityToDark { get; }
        public virtual HookPosition HookPosition { get; protected set; } = HookPosition.PostSync;
        public bool ForceSwitch { get; set; }
        public virtual bool Enabled
        {
            get { return Settings.Enabled; }
        }
        public void Switch(SwitchEventArgs e)
        {
            Logger.Trace($"switch invoked for {GetType().Name} ({Enum.GetName(HookPosition)})");
            ForceSwitch = false;
            if (Enabled)
            {
                if (!Initialized)
                {
                    RunEnableHook();
                }
                try
                {
                    HandleSwitch(e);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"uncaught exception in component {GetType().Name}, source: {ex.Source}, message: ");
                }
            }
            else if (Initialized)
            {
                RunDisableHook();
            }
        }

        protected virtual void UpdateSettingsState() { }

        /// <summary>
        /// Initializes the module if it has a hook specified. Does nothing otherwise.
        /// </summary>
        protected virtual void EnableHook() { }

        /// <summary>
        /// Deinitializes the module and restores the original state. Does nothing if no hook is specified.
        /// </summary>
        protected virtual void DisableHook() { }

        /// <summary>
        /// A callback method that is invoked after the component has run to its completion and a theme switch was performed. 
        /// Adm at this point is in a stable state with the new theme settings being available
        /// </summary>
        protected virtual void Callback(SwitchEventArgs e) { }

        /// <summary>
        /// True when the component should be compatible with the ThemeHandler switching mode
        /// </summary>
        public abstract bool ThemeHandlerCompatibility { get; }

        /// <summary>
        /// Entrypoint, called when a component needs to be updated
        /// </summary>
        /// <param name="newTheme">the new theme to apply</param>
        /// <param name="e">the switch event args</param>
        protected abstract void HandleSwitch(SwitchEventArgs e);
        /// <summary>
        /// Determines whether the component needs to be triggered to update to the correct system state
        /// </summary>
        /// <returns>true if the component needs to be executed; false otherwise</returns>
        protected abstract bool ComponentNeedsUpdate(SwitchEventArgs e);

        /// <summary>
        /// Executes the update settings state method
        /// </summary>
        /// <param name="newSettings"></param>
        public void RunUpdateSettingsState(object newSettings)
        {
            if (newSettings is ISwitchComponentSettings<T> temp)
            {
                bool isInit = Settings == null;
                SettingsBefore = Settings;
                Settings = temp;
                if (!isInit) UpdateSettingsState();
            }
            else
            {
                Logger.Error($"could not convert generic settings object to ${typeof(T)}, no settings update performed.");
            }
        }

        /// <summary>
        /// Executes the callback method
        /// </summary>
        public void RunCallback(SwitchEventArgs e)
        {
            Logger.Trace($"running callback for {GetType().Name}");
            Callback(e);
        }

        /// <summary>
        /// Executes the enable hook
        /// </summary>
        public void RunEnableHook()
        {
            Logger.Debug($"running enable hook for {GetType().Name}");
            try
            {
                EnableHook();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"error while running enable hook for {GetType().Name}");
            }
            Initialized = true;
        }

        /// <summary>
        /// Executes the disable hook
        /// </summary>
        public void RunDisableHook()
        {
            Logger.Debug($"running disable hook for {GetType().Name}");
            try
            {
                DisableHook();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"error while running disable hook for {GetType().Name}");
            }
            Initialized = false;
        }

        public bool RunComponentNeedsUpdate(SwitchEventArgs e)
        {
            try
            {
                return ComponentNeedsUpdate(e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"uncaught exception in component {GetType().Name}'s update rule, source: {ex.Source}, message: ");
            }
            return false;
        }
    }
}
