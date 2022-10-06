using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeLib.Interfaces;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class AppsSwitch : BaseComponent<AppsSwitchSettings>
    {
        private Theme currentComponentTheme;
        public AppsSwitch() : base() {
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
            Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }
    }
}
