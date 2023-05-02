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
using AutoDarkModeSvc.Handlers.ThemeFiles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.VisualStyles;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class AccentColorSwitch : BaseComponent<SystemSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => true;
        public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
        public override bool Enabled
        {
            get { return Settings.Component.DWMPrevalenceSwitch; }
        }

        private bool currentDWMColorActive;

        public AccentColorSwitch() : base() { }

        protected override void EnableHook()
        {
            try
            {
                currentDWMColorActive = RegistryHandler.IsDWMPrevalence();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't retrieve DWM prevalence state: ");
            }
        }

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (e.Theme == Theme.Dark)
            {
                if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark && !currentDWMColorActive)
                {
                    return true;
                }
                else if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Light && currentDWMColorActive)
                {
                    return true;
                }
            }
            else if (e.Theme == Theme.Light)
            {
                if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Light && !currentDWMColorActive)
                {
                    return true;
                }
                else if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark && currentDWMColorActive)
                {
                    return true;
                }
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            try
            {
                bool previousSetting = currentDWMColorActive;
                if (e.Theme == Theme.Dark && Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark)
                {
                    RegistryHandler.SetDWMPrevalence(1);
                    currentDWMColorActive = true;
                }
                else if (e.Theme == Theme.Light && Settings.Component.DWMPrevalenceEnableTheme == Theme.Light)
                {
                    RegistryHandler.SetDWMPrevalence(1);
                    currentDWMColorActive = true;
                }
                else
                {
                    RegistryHandler.SetDWMPrevalence(0);
                    currentDWMColorActive = false;
                }
                Logger.Info($"update info - previous: dwm prevalence {previousSetting.ToString().ToLower()}, now: {currentDWMColorActive.ToString().ToLower()}, mode: during {Enum.GetName(typeof(Theme), Settings.Component.DWMPrevalenceEnableTheme).ToString().ToLower()}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not toggle DWM prevalence: ");
            }
        }
    }
}
