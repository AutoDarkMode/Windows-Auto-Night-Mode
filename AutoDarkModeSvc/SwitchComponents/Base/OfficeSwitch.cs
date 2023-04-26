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
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using AutoDarkModeSvc.Events;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class OfficeSwitch : BaseComponent<OfficeSwitchSettings>
    {
        private Theme currentComponentTheme = Theme.Unknown;
        private int ChoosenLightTheme = 0;

        public override bool ThemeHandlerCompatibility { get; } = true;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if(currentComponentTheme == Theme.Unknown)
            {
                return true;
            }
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
            else if (Settings.Component.Mode == Mode.FollowSystemTheme && currentComponentTheme != Theme.Automatic)
            {
                return true;
            }
            else if (ChoosenLightTheme != Settings.Component.LightTheme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            try
            {
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    OfficeTheme(Settings.Component.DarkTheme);
                    currentComponentTheme = Theme.Dark;

                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    OfficeTheme(Settings.Component.LightTheme);
                    currentComponentTheme = Theme.Light;
                    ChoosenLightTheme = Settings.Component.LightTheme;
                }
                else if (Settings.Component.Mode == Mode.FollowSystemTheme)
                {
                    OfficeTheme(6);
                    currentComponentTheme = Theme.Automatic;
                    ChoosenLightTheme = 6;
                }
                else
                {
                    if (e.Theme == Theme.Dark)
                    {
                        OfficeTheme(Settings.Component.DarkTheme);
                    }
                    else
                    {
                        OfficeTheme(Settings.Component.LightTheme);
                    }
                    currentComponentTheme = e.Theme;
                    ChoosenLightTheme = Settings.Component.LightTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set office theme");
            }
            Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }

        /// <summary>
        /// Changes the office theme
        /// </summary>
        /// <param name="themeValue">0 = colorful, 3 = grey, 4 = black, 5 = white, 6 = follow_system</param>
        private static void OfficeTheme(byte themeValue)
        {
            string officeCommonKey = @"Software\Microsoft\Office\16.0\Common";

            //edit first registry key
            using RegistryKey commonKey = Registry.CurrentUser.OpenSubKey(officeCommonKey, true);
            commonKey.SetValue("UI Theme", themeValue);

            //search for the second key and then change it
            using RegistryKey identityKey = Registry.CurrentUser.OpenSubKey(officeCommonKey + @"\Roaming\Identities\", true);

            string msaSubkey = @"\Settings\1186\{00000000-0000-0000-0000-000000000000}\";
            string anonymousSubKey = msaSubkey + @"\PendingChanges";

            foreach (var v in identityKey.GetSubKeyNames())
            {
                //registry key for users logged in with msa
                if (!v.Equals("Anonymous"))
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + msaSubkey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + msaSubkey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
                //registry key for users without msa
                else
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + anonymousSubKey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + anonymousSubKey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
            }
        }
    }
}
