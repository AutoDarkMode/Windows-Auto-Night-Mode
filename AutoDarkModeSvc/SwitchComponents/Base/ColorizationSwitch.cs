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
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class ColorizationSwitch : BaseComponent<ColorizationSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override DwmRefreshType TriggersDwmRefresh => DwmRefreshType.Full;
        private bool invalidHexFound = false;
        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            bool autoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();
            if (e.Theme == Theme.Dark)
            {
                if (autoColorizationState != Settings.Component.DarkAutoColorization)
                {
                    return true;
                }
                else if (invalidHexFound && (Settings.Component.DarkAutoColorization == false))
                {
                    return false;
                }
                else if (!Settings.Component.DarkAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.DarkHex)
                {
                    return true;
                }
            }
            else if (e.Theme == Theme.Light)
            {
                if (autoColorizationState != Settings.Component.LightAutoColorization)
                {
                    return true;
                }
                else if (invalidHexFound && (Settings.Component.LightAutoColorization == false))
                {
                    return false;
                }
                else if (!Settings.Component.LightAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.LightHex)
                {
                    return true;
                }
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            var sortOrderAutoCol = GlobalState.ManagedThemeFile.VisualStyles.AutoColorization.Item2;
            var sortOrderColCol = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item2;
            bool prevAutoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();

            bool newAutoColorizationState = false;
            if (e.Theme == Theme.Dark)
            {
                if (Settings.Component.DarkAutoColorization) newAutoColorizationState = true;
                else newAutoColorizationState = false;
            }
            else if (e.Theme == Theme.Light)
            {
                if (Settings.Component.LightAutoColorization) newAutoColorizationState = true;
                else newAutoColorizationState = false;
            }            
            if (newAutoColorizationState) GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("1", sortOrderAutoCol);
            else GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("0", sortOrderAutoCol);

            if (prevAutoColorizationState != newAutoColorizationState)
            {
                Logger.Info($"update info - auto colorization - previous {(prevAutoColorizationState ? "enabled" : "disabled")}, " +
                    $"pending: {(newAutoColorizationState ? "enabled" : "disabled")} ({Enum.GetName(typeof(Theme), e.Theme)})");
            }

            // if auto colorization is enabled the hex value doesn't matter, so the rest can be skipped.
            // If auto col is or was disabled, then we need to process the hex values
            if (newAutoColorizationState == true)
            {
                return;
            }

            var oldColor = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#");

            string newHex;
            Regex hexValidator = new(Helper.Hegex);

            if (e.Theme == Theme.Dark) newHex = Settings.Component.DarkHex;
            else newHex = Settings.Component.LightHex;


            if (!hexValidator.IsMatch(newHex))
            {
                Logger.Warn($"an invalid hex color ({newHex}) found, updating colorization (accent color) disabled until the hex color has been fixed");
                invalidHexFound = true;
                return;
            }
            Logger.Info($"update info - color - previous: {oldColor}, pending: {newHex} ({Enum.GetName(typeof(Theme), e.Theme)})");
            newHex = newHex.Replace("#", "0X");
            GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor = (newHex, sortOrderColCol);
        }

        protected override void UpdateSettingsState()
        {
            Regex hexValidator = new(Helper.Hegex);
            if (invalidHexFound && hexValidator.IsMatch(Settings.Component.LightHex) && hexValidator.IsMatch(Settings.Component.DarkHex))
            {
                Logger.Info("invalid hex color has been corrected, component will now function again");
                invalidHexFound = false;
            }
        }

        protected override void Callback(SwitchEventArgs e)
        {
            bool darkAutoCol = Settings.Component.DarkAutoColorization;
            bool lightAutoCol = Settings.Component.LightAutoColorization;

            string accent = "";
            if (darkAutoCol || lightAutoCol)
            {
                try
                {
                    accent = RegistryHandler.GetAccentColor();
                }
                catch (Exception ex) 
                {
                    Logger.Error(ex, "failed getting accent color state to update config");
                    return;
                }
            }

            bool save = false;
            if (e.Theme == Theme.Dark && darkAutoCol)
            {
                Settings.Component.DarkHex = accent;
                save = true;
            }
            else if (e.Theme == Theme.Light && lightAutoCol)
            {
                Settings.Component.LightHex = accent;
                save = true;
            }
            if (save)
            {
                try
                {
                    GlobalState.SkipConfigFileReload = true;
                    AdmConfigBuilder.Instance().Save();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"error auto updating colorization value {accent} for {Enum.GetName(e.Theme)}");
                }
            }
        }
    }
}
