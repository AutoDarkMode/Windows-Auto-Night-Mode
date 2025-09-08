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
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;

namespace AutoDarkModeSvc.SwitchComponents.Base;

class SystemSwitchThemeFile : BaseComponent<SystemSwitchSettings>
{
    protected Theme currentComponentTheme = Theme.Unknown;
    public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
    public SystemSwitchThemeFile() : base() { }

    protected override void EnableHook()
    {
        RefreshRegkeys();
    }


    protected void RefreshRegkeys()
    {
        try
        {
            currentComponentTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "couldn't initialize system apps theme state");
        }
    }
    public override bool ThemeHandlerCompatibility => false;

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        if (Settings.Component.Mode == Mode.DarkOnly)
        {
            // Themes do not match
            if (currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            return false;

        }
        else if (Settings.Component.Mode == Mode.LightOnly)
        {
            if (currentComponentTheme != Theme.Light)
            {
                return true;
            }
            return false;
        }
        else if (Settings.Component.Mode == Mode.Switch)
        {
            // Themes do not match
            if (currentComponentTheme != e.Theme)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        SwitchSystemTheme(e.Theme);
    }

    protected virtual void SwitchSystemTheme(Theme newTheme)
    {
        string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
        try
        {
            if (Settings.Component.Mode == Mode.LightOnly)
            {
                SwitchLightOnly();
            }
            else if (Settings.Component.Mode == Mode.DarkOnly)
            {
                SwitchDarkOnly();
            }
            else
            {
                SwitchAdaptive(newTheme);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not set system theme");
        }
        string accentInfo;
        if (Settings.Component.Mode == Mode.AccentOnly)
        {
            accentInfo = $"on {Enum.GetName(typeof(Theme), Settings.Component.TaskbarColorDuring).ToLower()}";
        }
        else
        {
            accentInfo = Settings.Component.TaskbarColorSwitch ? "yes" : "no";
        }
        Logger.Info($"update info - previous: {oldTheme}, " +
            $"pending: {Enum.GetName(typeof(Theme), currentComponentTheme)}, " +
            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
            $"accent: {accentInfo}");
    }

    protected void SwitchLightOnly()
    {

        if (currentComponentTheme != Theme.Dark)
        {
            ThemeFile themeFile = GlobalState.ManagedThemeFile;
            themeFile.VisualStyles.SystemMode = (nameof(Theme.Light), themeFile.VisualStyles.SystemMode.Item2);
        }
        currentComponentTheme = Theme.Light;
    }

    protected void SwitchDarkOnly()
    {
        if (currentComponentTheme != Theme.Dark)
        {
            ThemeFile themeFile = GlobalState.ManagedThemeFile;
            themeFile.VisualStyles.SystemMode = (nameof(Theme.Dark), themeFile.VisualStyles.SystemMode.Item2);
        }
        currentComponentTheme = Theme.Dark;
    }

    protected void SwitchAdaptive(Theme newTheme)
    {
        ThemeFile themeFile = GlobalState.ManagedThemeFile;
        if (newTheme == Theme.Light)
        {
            themeFile.VisualStyles.SystemMode = (nameof(Theme.Light), themeFile.VisualStyles.SystemMode.Item2);
        }
        else if (newTheme == Theme.Dark)
        {
            themeFile.VisualStyles.SystemMode = (nameof(Theme.Dark), themeFile.VisualStyles.SystemMode.Item2);
        }
        currentComponentTheme = newTheme;
    }

    protected override void UpdateSettingsState()
    {
        RefreshRegkeys();
    }


}
