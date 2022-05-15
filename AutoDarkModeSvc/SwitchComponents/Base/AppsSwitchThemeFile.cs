using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeConfig.Interfaces;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class AppsSwitchThemeFile : BaseComponent<AppsSwitchSettings>
    {
        private Theme currentComponentTheme;
        public AppsSwitchThemeFile() : base() {
            try
            {
                currentComponentTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize apps theme state");
            }
        }

        public override bool ThemeHandlerCompatibility { get; } = false;

        public override bool ComponentNeedsUpdate(Theme newTheme)
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

        protected override void HandleSwitch(Theme newTheme, SwitchEventArgs e)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            newTheme = newTheme == Theme.Dark ? Theme.Dark : Theme.Light;
            ThemeFile themeFile = GlobalState.ManagedThemeFile;

            if (Settings.Component.Mode == Mode.DarkOnly)
            {
                themeFile.VisualStyles.AppMode = (nameof(Theme.Dark), themeFile.VisualStyles.AppMode.Item2);
                currentComponentTheme = Theme.Dark;
            }
            else if (Settings.Component.Mode == Mode.LightOnly)
            {
                themeFile.VisualStyles.AppMode = (nameof(Theme.Light), themeFile.VisualStyles.AppMode.Item2);
                currentComponentTheme = Theme.Light;
            }
            else
            {
                themeFile.VisualStyles.AppMode = (Enum.GetName(typeof(Theme), newTheme), themeFile.VisualStyles.AppMode.Item2);
                currentComponentTheme = newTheme;
            }
            Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }
    }
}
