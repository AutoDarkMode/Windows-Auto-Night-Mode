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

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    /// <summary>
    /// This class is a special case for the AppsSwitchThemeFile component, because on Windows builds older than 21H2 we use the legacy theme switching method
    /// </summary>
    class AppsSwitch : AppsSwitchThemeFile
    {
        protected override void HandleSwitch(SwitchEventArgs e)
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
                    RegistryHandler.SetAppsTheme((int)e.Theme);
                    currentComponentTheme = e.Theme;
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
