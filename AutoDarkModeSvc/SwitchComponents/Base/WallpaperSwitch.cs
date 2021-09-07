using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class WallpaperSwitch : BaseComponent<WallpaperSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => true;
        private Theme currentComponentTheme = Theme.Undefined;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            try
            {
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    currentComponentTheme = Theme.Dark;
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    currentComponentTheme = Theme.Light;
                }
                else
                {
                    currentComponentTheme = newTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set wallpapers");
            }
            Logger.Info($"update info - previous: {oldTheme}, current: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }

        public override void EnableHook()
        {
            WallpaperHandler.GetWallpapers();
            base.EnableHook();
        }

        
    }
}
