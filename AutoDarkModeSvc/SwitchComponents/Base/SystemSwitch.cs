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
    /// <summary>
    /// This class is a special case for the SwitchSystemThemeFile component, because on Windows builds older than 21H2 we use the legacy theme switching method
    /// </summary>
    class SystemSwitch : SystemSwitchThemeFile
    {
        public override bool ThemeHandlerCompatibility { get; } = false;
        protected override async Task SwitchSystemTheme(Theme newTheme)
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
            Logger.Info($"update info - previous: {oldTheme}/{(oldAccent ? "accent" : "NoAccent")}, " +
                $"now: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{(currentTaskbarColorActive ? "Accent" : "NoAccent")}, " +
                $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
                $"accent: {accentInfo}");
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
    }
}
