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

namespace AutoDarkModeSvc.SwitchComponents.Base;

/// <summary>
/// This class is a special case for the SwitchSystemThemeFile component, because on Windows builds older than 21H2 we use the legacy theme switching method
/// </summary>
class SystemSwitch : BaseComponent<SystemSwitchSettings>
{
    protected Theme currentComponentTheme = Theme.Unknown;
    protected bool themeModeEnabled;
    protected bool currentTaskbarColorActive;
    public override DwmRefreshType NeedsDwmRefresh => DwmRefreshType.Standard;
    public SystemSwitch() : base() { }

    protected override void EnableHook()
    {
        RefreshRegkeys();
    }

    protected void RefreshRegkeys()
    {
        try
        {
            currentComponentTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;
            currentTaskbarColorActive = RegistryHandler.IsTaskbarColor();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "couldn't initialize system apps theme state");
        }
    }

    public override bool ThemeHandlerCompatibility { get; } = false;

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        if (Settings.Component.Mode == Mode.AccentOnly)
        {
            // if theme does not match dark we need to report true, as accent color isn't available in light mode
            // Do not return true on windows theme mode, as this would potentially modify the theme
            if (currentComponentTheme != Theme.Dark && !themeModeEnabled) return true;

            if (e.Theme == Theme.Dark)
            {
                // allow toggling of the taskbar color in dark mode if it is not active yet, or still active
                if (Settings.Component.TaskbarColorDuring == Theme.Dark && !currentTaskbarColorActive) return true;
                else if (Settings.Component.TaskbarColorDuring == Theme.Light && currentTaskbarColorActive) return true;
            }
            else if (e.Theme == Theme.Light)
            {
                // allow toggling of the taskbar color in light mode if it is not active yet, or still active (inverse of Theme.Dark if clause)
                if (Settings.Component.TaskbarColorDuring == Theme.Dark && currentTaskbarColorActive) return true;
                else if (Settings.Component.TaskbarColorDuring == Theme.Light && !currentTaskbarColorActive) return true;
            }
            return false;
        }
        else if (Settings.Component.Mode == Mode.DarkOnly)
        {
            // Themes do not match
            if (currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            // Task bar accent color is disabled, but still active
            else if (!Settings.Component.TaskbarColorSwitch && currentTaskbarColorActive)
            {
                return true;
            }
            // task bar accent color should switch, and taskbar color hasn't switched yet
            else if (Settings.Component.TaskbarColorSwitch && !currentTaskbarColorActive)
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
            // Task bar accent color should switch, target is light mode and the taskbar color hasn't switched yet
            else if (Settings.Component.TaskbarColorSwitch && currentTaskbarColorActive && e.Theme == Theme.Light)
            {
                return true;
            }
            // Task bar accent color is disabled, but still active
            else if (!Settings.Component.TaskbarColorSwitch && currentTaskbarColorActive)
            {
                return true;
            }
            // task bar accent color should switch, target is dark mode and taskbar color hasn't switched yet
            else if (Settings.Component.TaskbarColorSwitch && !currentTaskbarColorActive && e.Theme == Theme.Dark)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        Task.Run(async () => { await SwitchSystemTheme(e.Theme); }).Wait();
    }

    protected async Task SwitchSystemTheme(Theme newTheme)
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
            accentInfo = $"on {Enum.GetName(typeof(Theme), Settings.Component.TaskbarColorDuring).ToLower()}";
        }
        else
        {
            accentInfo = Settings.Component.TaskbarColorSwitch ? "yes" : "no";
        }
        Logger.Info($"update info - previous: {oldTheme}/{(oldAccent ? "accent" : "NoAccent")}, " +
            $"now: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{(currentTaskbarColorActive ? "Accent" : "NoAccent")}, " +
            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
            $"accent: {accentInfo}");
    }

    private async Task SwitchLightOnly(int taskdelay)
    {
        if (Settings.Component.TaskbarColorSwitch)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
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
        if (Settings.Component.TaskbarColorSwitch)
        {
            RegistryHandler.SetTaskbarColorPrevalence(1);
            currentTaskbarColorActive = true;
        }
        else if (!Settings.Component.TaskbarColorSwitch && currentTaskbarColorActive)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
            currentTaskbarColorActive = false;
        }
    }

    private async Task SwitchAdaptive(Theme newTheme, int taskdelay)
    {
        if (newTheme == Theme.Light)
        {
            RegistryHandler.SetTaskbarColorPrevalence(0);
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
            if (Settings.Component.TaskbarColorSwitch)
            {
                RegistryHandler.SetTaskbarColorPrevalence(1);
                currentTaskbarColorActive = true;
            }
            else if (!Settings.Component.TaskbarColorSwitch && currentTaskbarColorActive)
            {
                RegistryHandler.SetTaskbarColorPrevalence(0);
                currentTaskbarColorActive = false;
            }
        }
        currentComponentTheme = newTheme;
    }

    protected async Task SwitchAccentOnly(Theme newTheme, int taskdelay)
    {
        if (currentComponentTheme != Theme.Dark && !themeModeEnabled)
        {
            RegistryHandler.SetSystemTheme((int)Theme.Dark);
        }
        else
        {
            taskdelay = 0;
        }
        await Task.Delay(taskdelay);

        if (newTheme == Theme.Dark)
        {
            if (Settings.Component.TaskbarColorDuring == Theme.Dark)
            {
                RegistryHandler.SetTaskbarColorPrevalence(1);
                currentTaskbarColorActive = true;
            }
            else
            {
                RegistryHandler.SetTaskbarColorPrevalence(0);
                currentTaskbarColorActive = false;
            }
        }
        else if (newTheme == Theme.Light)
        {
            if (Settings.Component.TaskbarColorDuring == Theme.Light)
            {
                RegistryHandler.SetTaskbarColorPrevalence(1);
                currentTaskbarColorActive = true;
            }
            else
            {
                RegistryHandler.SetTaskbarColorPrevalence(0);
                currentTaskbarColorActive = false;
            }
        }
        currentComponentTheme = Theme.Dark;
    }
}
