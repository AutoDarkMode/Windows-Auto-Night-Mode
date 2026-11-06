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
using System;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class AccentColorSwitch : BaseComponent<SystemSwitchSettings>
{
    public override bool ThemeHandlerCompatibility => true;
    public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
    public override bool Enabled => Settings.Component.DWMPrevalenceSwitch;

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
        switch (e.Theme)
        {
            case Theme.Light:
                if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Light && !currentDWMColorActive)
                {
                    return true;
                }
                else if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark && currentDWMColorActive)
                {
                    return true;
                }
                break;
            case Theme.Dark:
                if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark && !currentDWMColorActive)
                {
                    return true;
                }
                else if (Settings.Component.DWMPrevalenceEnableTheme == Theme.Light && currentDWMColorActive)
                {
                    return true;
                }
                break;
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        try
        {
            bool previousSetting = currentDWMColorActive;
            switch (e.Theme)
            {
                case Theme.Light when Settings.Component.DWMPrevalenceEnableTheme == Theme.Light:
                    RegistryHandler.SetDWMPrevalence(1);
                    currentDWMColorActive = true;
                    break;
                case Theme.Dark when Settings.Component.DWMPrevalenceEnableTheme == Theme.Dark:
                    RegistryHandler.SetDWMPrevalence(1);
                    currentDWMColorActive = true;
                    break;
                default:
                    RegistryHandler.SetDWMPrevalence(0);
                    currentDWMColorActive = false;
                    break;
            }
            Logger.Info($"update info - previous: dwm prevalence {previousSetting.ToString().ToLower()}, now: {currentDWMColorActive.ToString().ToLower()}, mode: during {Enum.GetName(typeof(Theme), Settings.Component.DWMPrevalenceEnableTheme).ToString().ToLower()}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not toggle DWM prevalence: ");
        }
    }
}
