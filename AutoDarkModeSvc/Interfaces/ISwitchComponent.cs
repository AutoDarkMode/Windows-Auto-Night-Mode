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
using AutoDarkModeLib;
using AutoDarkModeLib.Interfaces;
using AutoDarkModeSvc.Events;

namespace AutoDarkModeSvc.Interfaces
{
    interface ISwitchComponent
    {
        /// <summary>
        /// Returns if the module is enabled
        /// </summary>
        /// <returns>true if module is enabled; false otherwise</returns>
        public bool Enabled { get; }
        /// <summary>
        /// Calling this method will trigger a theme switch if the component is enabled and initialized.<br></br>
        /// The Init hook is called if the component has not been initialized yet.<br></br>
        /// If the component is disabled, and still initialized, the deinit hook will be called.<br></br>
        /// If the component is disabled and properly deinitialized, nothing will happen.
        /// </summary>
        /// <param name="e">the event args from the event source, containing the new theme and information about the switch</param>
        public void Switch(SwitchEventArgs e);

        /// <summary>
        /// Checks if the component needs to be updated according to its internal state
        /// </summary>
        /// <param name="newTheme">The requested new theme</param>
        /// <returns>true if the component needs to be updated; false otherwise</returns>
        public bool RunComponentNeedsUpdate(SwitchEventArgs e);
        /// <summary>
        /// Refreshes the local copy of the component settings. Should be called before invoking Switch() to make sure the config is up to date
        /// </summary>
        /// <param name="newSettings">the correct settings object for the switch component. Using the wrong object will result in no update.</param>
        public void RunUpdateSettingsState(object newSettings);
        /// <summary>
        /// Checks if the component needs to be updated, i.e Switch() needs to be called
        /// </summary>
        public int PriorityToDark { get; }
        /// <summary>
        /// Priority for switching to light mode
        /// </summary>
        public int PriorityToLight { get; }
        /// <summary>
        /// Determines whether the component should be called before or after a theme file synchronization
        /// </summary>
        public HookPosition HookPosition { get; }
        /// <summary>
        /// Initializes the module if it has a hook specified. Does nothing otherwise.
        /// </summary>
        public void RunEnableHook();
        /// <summary>
        /// Deinitializes the module and restores the original state. Does nothing if no hook is specified.
        /// </summary>
        public void RunDisableHook();
        /// <summary>
        /// Executes the callback function of the component
        /// </summary>
        public void RunCallback(SwitchEventArgs e);
        /// <summary>
        /// Determines if the module requires dwm refresh
        /// </summary>
        public DwmRefreshType NeedsDwmRefresh { get; }
        /// <summary>
        /// Determines the quality of DWM refresh a module performs
        /// </summary>
        public DwmRefreshType TriggersDwmRefresh { get; }
        /// <summary>
        /// Determines if the module can be run with the windows theme switcher
        /// </summary>
        /// <returns></returns>
        public bool ThemeHandlerCompatibility { get; }
        /// <summary>
        /// Check if the component has been properly initialized. Can be used to trigger hooks on disable/enable
        /// </summary>
        public bool Initialized { get; }
        /// <summary>
        /// If this flag is set, a theme switch will be forced.
        /// This will deactivate itself after performing this operation once
        /// </summary>
        public bool ForceSwitch { get; set; }

    }
}
