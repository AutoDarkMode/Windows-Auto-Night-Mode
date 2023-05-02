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
using AutoDarkModeSvc.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class CursorSwitch : BaseComponent<CursorSwitchSettings>
    {
        private Theme currentTheme = Theme.Unknown;
        public override bool ThemeHandlerCompatibility => false;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (currentTheme != e.Theme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            Theme oldTheme = currentTheme;
            currentTheme = e.Theme;
            string cursorSchemeNew = "";
            if (e.Theme == Theme.Light)
            {
                cursorSchemeNew = Settings.Component.CursorsLight;
            }
            else if (e.Theme == Theme.Dark)
            {
                cursorSchemeNew = Settings.Component.CursorsDark;
            }

            if (cursorSchemeNew != null && cursorSchemeNew.Length > 0)
            {
                GlobalState.ManagedThemeFile.Cursors = RegistryHandler.GetCursorScheme(cursorSchemeNew);
                Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), e.Theme)} ({cursorSchemeNew})");
            }
            else
            {
                GlobalState.ManagedThemeFile.Cursors = RegistryHandler.GetCursors();
                Logger.Info("update info - no cursors selected, setting current default cursor");
            }
        }

        protected override void EnableHook()
        {
            try
            {
                Cursors current = RegistryHandler.GetCursors();
                if (Settings.Component.CursorsLight == Settings.Component.CursorsDark && current.DefaultValue.Item1 == Settings.Component.CursorsLight)
                {
                    currentTheme = GlobalState.InternalTheme;
                }
                else if (current.DefaultValue.Item1 == Settings.Component.CursorsLight)
                {
                    currentTheme = Theme.Light;
                }
                else if (current.DefaultValue.Item1 == Settings.Component.CursorsDark)
                {
                    currentTheme = Theme.Dark;
                }
                else
                {
                    currentTheme = Theme.Unknown;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not retrieve currently active cursors:");
            }
        }

        protected override void UpdateSettingsState()
        {
            EnableHook();
        }
    }
}
