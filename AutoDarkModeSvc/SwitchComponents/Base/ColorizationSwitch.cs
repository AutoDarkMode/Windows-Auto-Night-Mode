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
using System.Text.RegularExpressions;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class ColorizationSwitch : BaseComponent<ColorizationSwitchSettings>
{
    // ### State ###

    private bool invalidHexFound = false;

    // ### Component overrides ###

    public override bool ThemeHandlerCompatibility => false;

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        bool autoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();
        switch (e.Theme)
        {
            case Theme.Light:
                if (autoColorizationState != Settings.Component.LightAutoColorization)
                {
                    return true;
                }
                else if (invalidHexFound && Settings.Component.LightAutoColorization == false)
                {
                    return false;
                }
                else if (!Settings.Component.LightAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.LightHex)
                {
                    return true;
                }
                break;
            case Theme.Dark:
                if (autoColorizationState != Settings.Component.DarkAutoColorization)
                {
                    return true;
                }
                else if (invalidHexFound && Settings.Component.DarkAutoColorization == false)
                {
                    return false;
                }
                else if (!Settings.Component.DarkAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.DarkHex)
                {
                    return true;
                }
                break;
        }
        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        var sortOrderAutoCol = GlobalState.ManagedThemeFile.VisualStyles.AutoColorization.Item2;
        var sortOrderColCol = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item2;
        bool prevAutoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();

        bool newAutoColorizationState = false;
        switch (e.Theme)
        {
            case Theme.Light:
                newAutoColorizationState = Settings.Component.LightAutoColorization;
                break;
            case Theme.Dark:
                newAutoColorizationState = Settings.Component.DarkAutoColorization;
                break;
        }

        if (newAutoColorizationState) GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("1", sortOrderAutoCol);
        else GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("0", sortOrderAutoCol);

        if (prevAutoColorizationState != newAutoColorizationState)
        {
            Logger.Info($"update info - auto colorization - previous {(prevAutoColorizationState ? "enabled" : "disabled")}, " +
                $"pending: {(newAutoColorizationState ? "enabled" : "disabled")} ({Enum.GetName(e.Theme)})");
        }

        // if auto colorization is enabled the hex value doesn't matter, so the rest can be skipped.
        // If auto col is or was disabled, then we need to process the hex values
        if (newAutoColorizationState == true)
        {
            return;
        }

        var oldColor = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#");
        Regex hexValidator = new(Helper.Hegex);
        var newHex = e.Theme switch
        {
            Theme.Dark => Settings.Component.DarkHex,
            _ => Settings.Component.LightHex,
        };

        if (!hexValidator.IsMatch(newHex))
        {
            Logger.Warn($"an invalid hex color ({newHex}) found, updating colorization (accent color) disabled until the hex color has been fixed");
            invalidHexFound = true;
            return;
        }

        Logger.Info($"update info - color - previous: {NormalizeHex(oldColor)}, pending: {NormalizeHex(newHex)} ({Enum.GetName(e.Theme)})");
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
        switch (e.Theme)
        {
            case Theme.Light when lightAutoCol:
                Settings.Component.LightHex = accent;
                save = true;
                break;
            case Theme.Dark when darkAutoCol:
                Settings.Component.DarkHex = accent;
                save = true;
                break;
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

    // ### Helpers ###

    /*
     * Converts #AARRGGBB to #RRGGBB for logging/display.
     * Returns the original value for any other format.
     */
    private static string NormalizeHex(string hex)
    {
        return hex != null && hex.StartsWith('#') && hex.Length == 9
            ? string.Concat("#", hex.AsSpan(3))
            : hex;
    }
}
