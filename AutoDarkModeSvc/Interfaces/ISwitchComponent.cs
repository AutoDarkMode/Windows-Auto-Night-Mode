using AutoDarkModeConfig;
using AutoDarkModeConfig.Interfaces;

namespace AutoDarkModeSvc.Interfaces
{
    interface ISwitchComponent
    {
        /// <summary>
        /// Returns if the module is enabled
        /// </summary>
        /// <returns>true if module is enabled; false otherwise</returns>
        public bool Enabled();
        /// <summary>
        /// Switches to the desired theme
        /// </summary>
        /// <param name="newTheme">the new theme that should be set</param>
        public void Switch(Theme newTheme);
        /// <summary>
        /// Refreshes the local copy of the component settings. Should be called before invoking Switch() to make sure the config is up to date
        /// </summary>
        /// <param name="newSettings">the correct settings object for the switch component. Using the wrong object will result in no update.</param>
        public void UpdateSettingsState(object newSettings);
        /// <summary>
        /// Checks if the component needs to be updated, i.e Switch() needs to be called
        /// </summary>
        /// <returns></returns>
        public int PriorityToDark();
        /// <summary>
        /// Priority for switching to light mode
        /// </summary>
        public int PriorityToLight();
        /// <summary>
        /// Initializes the module if necessary
        /// </summary>
        public void EnableHook();
        /// <summary>
        /// Determines if the module can be run with the windows theme switcher
        /// </summary>
        /// <returns></returns>
        public bool ThemeHandlerCompatibility { get; }

    }
}
