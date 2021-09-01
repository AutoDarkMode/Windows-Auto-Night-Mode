using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeConfig.Interfaces;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class AppSwitch : BaseComponent<AppSwitchSettings>
    {
        private Theme currentComponentTheme = Theme.Undefined;
        public AppSwitch() : base() { }

        protected override bool ThemeHandlerCompatibility { get; } = false;

        protected override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (Settings.Component.Mode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.LightOnly && currentComponentTheme != Theme.Light)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch && currentComponentTheme != newTheme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            try
            {
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    RegistryHandler.SetAppsTheme((int)Theme.Dark);
                    currentComponentTheme = Theme.Dark;
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    RegistryHandler.SetAppsTheme((int)Theme.Light);
                    currentComponentTheme = Theme.Light;
                }
                else
                {
                    RegistryHandler.SetAppsTheme((int)newTheme);
                    currentComponentTheme = newTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set apps theme");
            }
        }
    }
}
