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
using AutoDarkModeSvc.Handlers.ThemeFiles;

namespace AutoDarkModeSvc.SwitchComponents.Base;

class SystemSwitch : BaseComponent<SystemSwitchSettings>
{
    protected Theme currentComponentTheme = Theme.Unknown;
    public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
    public SystemSwitch() : base() { }

    protected override void EnableHook()
    {
        RefreshRegistryKeys();
    }


    protected void RefreshRegistryKeys()
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
        return Settings.Component.Mode switch
        {
            Mode.DarkOnly => currentComponentTheme != Theme.Dark,// Themes do not match
            Mode.LightOnly => currentComponentTheme != Theme.Light,
            Mode.Switch => currentComponentTheme != e.Theme,
            _ => false,
        };
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        SwitchSystemTheme(e.Theme);
        EnsureTaskbarColorDisabledInLight();
    }

    private void EnsureTaskbarColorDisabledInLight()
    {
        // when the taskbar color switch is enabled it manages color prevalence itself
        if (Settings.Component.TaskbarColorSwitch) return;

        if (currentComponentTheme != Theme.Light) return;

        try
        {
            if (RegistryHandler.IsTaskbarColor())
            {
                RegistryHandler.SetTaskbarColorPrevalence(0);
                Logger.Info("disabled taskbar color prevalence in light mode (taskbar color switch is off)");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not disable taskbar color prevalence in light mode: ");
        }
    }

    protected virtual void SwitchSystemTheme(Theme newTheme)
    {
        string oldTheme = Enum.GetName(currentComponentTheme);
        try
        {
            switch (Settings.Component.Mode)
            {
                case Mode.LightOnly:
                    SwitchLightOnly();
                    break;
                case Mode.DarkOnly:
                    SwitchDarkOnly();
                    break;
                default:
                    SwitchAdaptive(newTheme);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not set system theme");
        }

        var accentInfo = Settings.Component.Mode switch
        {
            Mode.AccentOnly => $"on {Enum.GetName(Settings.Component.TaskbarColorDuring).ToLower()}",
            _ => Settings.Component.TaskbarColorSwitch ? "yes" : "no",
        };

        Logger.Info($"update info - previous: {oldTheme}, " +
            $"pending: {Enum.GetName(currentComponentTheme)}, " +
            $"mode: {Enum.GetName(Settings.Component.Mode)}, " +
            $"accent: {accentInfo}");
    }

    protected void SwitchLightOnly()
    {

        if (currentComponentTheme != Theme.Light)
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
        switch (newTheme)
        {
            case Theme.Light:
                themeFile.VisualStyles.SystemMode = (nameof(Theme.Light), themeFile.VisualStyles.SystemMode.Item2);
                break;
            case Theme.Dark:
                themeFile.VisualStyles.SystemMode = (nameof(Theme.Dark), themeFile.VisualStyles.SystemMode.Item2);
                break;
        }
        currentComponentTheme = newTheme;
    }

    protected override void UpdateSettingsState()
    {
        RefreshRegistryKeys();
    }

}
