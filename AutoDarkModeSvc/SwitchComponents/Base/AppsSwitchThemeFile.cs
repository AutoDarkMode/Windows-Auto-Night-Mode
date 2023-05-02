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
using AutoDarkModeLib.Interfaces;
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
        protected Theme currentComponentTheme;
        public AppsSwitchThemeFile() : base() { }

        protected override void EnableHook()
        {
            try
            {
                currentComponentTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize apps theme state");
            }
        }
        public override DwmRefreshType TriggersDwmRefresh => DwmRefreshType.Standard;
        public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
        public override bool ThemeHandlerCompatibility { get; } = false;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (Settings.Component.Mode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.LightOnly && currentComponentTheme != Theme.Light)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch && currentComponentTheme != e.Theme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
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
                themeFile.VisualStyles.AppMode = (Enum.GetName(typeof(Theme), e.Theme), themeFile.VisualStyles.AppMode.Item2);
                currentComponentTheme = e.Theme;
            }
            Logger.Info($"update info - previous: {oldTheme}, pending: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }
    }
}
