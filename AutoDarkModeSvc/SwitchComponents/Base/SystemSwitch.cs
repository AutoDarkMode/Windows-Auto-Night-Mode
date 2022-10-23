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
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class SystemSwitch : BaseComponent<SystemSwitchSettings>
    {
        private Theme currentComponentTheme = Theme.Unknown;
        private bool themeModeEnabled;
        private bool currentTaskbarColorActive;
        public SystemSwitch() : base() { }

        public override void EnableHook()
        {
            try
            {
                currentComponentTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;
                currentTaskbarColorActive = RegistryHandler.IsColorPrevalence();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize system apps theme state");
            }
            base.EnableHook();
        }

        public override bool ThemeHandlerCompatibility => true;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (Settings.Component.Mode == Mode.AccentOnly)
            {
                // theme does not match dark, as accent color is avialable in dark only
                // Do not return true on windows theme mode, as this would potentially modify the theme
                if (currentComponentTheme != newTheme && !themeModeEnabled) return true;

                if (newTheme == Theme.Dark)
                {
                    // allow toggling of the taskbar color in dark mode if it is not active yet, or still active
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && !currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && currentTaskbarColorActive) return true;
                }
                else if (newTheme == Theme.Light)
                {
                    // allow toggling of the taskbar color in light mode if it is not active yet, or still active (inverse of Theme.Dark if clause)
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && !currentTaskbarColorActive) return true;
                }
                return false;
            }

            if (themeModeEnabled)
            {
                return false;
            }
            else if (Settings.Component.Mode == Mode.DarkOnly)
            {
                // Themes do not match
                if (currentComponentTheme != Theme.Dark)
                {
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive)
                {
                    return true;
                }
                return false;

            }
            else if (Settings.Component.Mode == Mode.LightOnly)
            {
                if (currentComponentTheme != Theme.Light) return true;
                return false;
            }
            else if (Settings.Component.Mode == Mode.Switch)
            {
                // Themes do not match
                if (currentComponentTheme != newTheme)
                {
                    return true;
                }
                // Task bar accent color should switch, target is light mode and the taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive && newTheme == Theme.Light)
                {
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, target is dark mode and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive && newTheme == Theme.Dark)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme, SwitchEventArgs e)
        {
            Task.Run(async() => { await SwitchSystemTheme(newTheme);}).Wait();
        }

        private async Task SwitchSystemTheme(Theme newTheme)
        {
            bool oldAccent = currentTaskbarColorActive;
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            int taskdelay = Settings.Component.TaskbarSwitchDelay;
            try
            {
                // Set system theme
                if (Settings.Component.Mode == Mode.AccentOnly)
                {
                    await SwitchAccentOnly(newTheme, taskdelay);
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    await SwitchLightOnly(taskdelay);
                }
                else if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    await SwitchDarkOnly(taskdelay);
                }
                else
                {
                    await SwitchAdaptive(newTheme, taskdelay);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set system theme");
            }
            string accentInfo;
            if (Settings.Component.Mode == Mode.AccentOnly)
            {
                accentInfo = $"on {Enum.GetName(typeof(Theme), Settings.Component.TaskbarColorWhenNonAdaptive).ToLower()}";
            }
            else
            {
                accentInfo = Settings.Component.TaskbarColorOnAdaptive ? "yes" : "no";
            }
            Logger.Info($"update info - previous: {oldTheme}/{(oldAccent ? "Accent" : "NoAccent")}, " +
                $"now: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{(currentTaskbarColorActive ? "Accent" : "NoAccent")}, " +
                $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
                $"accent: {accentInfo}");
        }

        private async Task SwitchAccentOnly(Theme newTheme, int taskdelay)
        {
            if (currentComponentTheme != Theme.Dark && !themeModeEnabled)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
            }
            else
            {
                taskdelay = 0;
            }
            await Task.Delay(taskdelay);

            if (newTheme == Theme.Dark)
            {
                if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            else if (newTheme == Theme.Light)
            {
                if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            currentComponentTheme = Theme.Dark;
        }

        private async Task SwitchLightOnly(int taskdelay)
        {
            if (Settings.Component.TaskbarColorOnAdaptive)
            {
                RegistryHandler.SetColorPrevalence(0);
                await Task.Delay(taskdelay);
            }
            currentTaskbarColorActive = false;
            RegistryHandler.SetSystemTheme((int)Theme.Light);
            currentComponentTheme = Theme.Light;
        }

        private async Task SwitchDarkOnly(int taskdelay)
        {
            if (currentComponentTheme != Theme.Dark)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
            }
            else
            {
                taskdelay = 0;
            }
            currentComponentTheme = Theme.Dark;
            await Task.Delay(taskdelay);
            if (Settings.Component.TaskbarColorOnAdaptive)
            {
                RegistryHandler.SetColorPrevalence(1);
                currentTaskbarColorActive = true;
            }
            else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
            {
                RegistryHandler.SetColorPrevalence(0);
                currentTaskbarColorActive = false;
            }
        }

        private async Task SwitchAdaptive(Theme newTheme, int taskdelay)
        {
            if (newTheme == Theme.Light)
            {
                RegistryHandler.SetColorPrevalence(0);
                currentTaskbarColorActive = false;
                await Task.Delay(taskdelay);
                RegistryHandler.SetSystemTheme((int)newTheme);
            }
            else if (newTheme == Theme.Dark)
            {
                if (currentComponentTheme != Theme.Dark)
                {
                    RegistryHandler.SetSystemTheme((int)Theme.Dark);
                }
                else
                {
                    taskdelay = 0;
                }
                currentComponentTheme = Theme.Dark;
                await Task.Delay(taskdelay);
                if (Settings.Component.TaskbarColorOnAdaptive)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            currentComponentTheme = newTheme;
        }

        public override void UpdateSettingsState(object newSettings)
        {
            base.UpdateSettingsState(newSettings);
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            themeModeEnabled = builder.Config.WindowsThemeMode.Enabled;
        }
    }
}
