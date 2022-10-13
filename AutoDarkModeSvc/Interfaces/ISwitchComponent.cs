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
        /// <param name="newTheme">the nwe theme that should be set</param>
        /// <param name="e">the event args from the event source</param>
        public void Switch(Theme newTheme, SwitchEventArgs e);

        /// <summary>
        /// Checks if the component needs to be updated according to its internal state
        /// </summary>
        /// <param name="newTheme">The requested new theme</param>
        /// <returns>true if the component needs to be updated; false otherwise</returns>
        public bool ComponentNeedsUpdate(Theme newTheme);
        /// <summary>
        /// Refreshes the local copy of the component settings. Should be called before invoking Switch() to make sure the config is up to date
        /// </summary>
        /// <param name="newSettings">the correct settings object for the switch component. Using the wrong object will result in no update.</param>
        public void UpdateSettingsState(object newSettings);
        /// <summary>
        /// Checks if the component needs to be updated, i.e Switch() needs to be called
        /// </summary>
        /// <returns></returns>
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
        /// Initializes the module if necessary
        /// </summary>
        public void EnableHook();
        /// <summary>
        /// Deinitializes the module and restores the original state
        /// </summary>
        /// <returns></returns>
        public void DisableHook();
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
