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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AutoDarkModeLib;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base;

class ColorFilterSwitch : BaseComponent<object>
{
    private bool currentColorFilterActive;
    public ColorFilterSwitch() : base() { }
    public override bool ThemeHandlerCompatibility => true;
    protected override void EnableHook()
    {
        try
        {
            RegistryHandler.ColorFilterSetup();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "failed to initialize color filter");
        }

        try
        {
            currentColorFilterActive = RegistryHandler.IsColorFilterActive();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "couldn't initialize color filter state");
        }

    }
    protected override void DisableHook()
    {
        if (!Settings.Enabled && currentColorFilterActive)
        {
            RegistryHandler.SetColorFilter(false);
            LaunchAtBroker();
            currentColorFilterActive = false;
        }
    }
    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        if (!currentColorFilterActive && e.Theme == Theme.Dark)
        {
            return true;
        }
        else if (currentColorFilterActive && e.Theme == Theme.Light)
        {
            return true;
        }
        return false;
    }

    private void LaunchAtBroker()
    {
        using Process atBrokerColorFilter = new();
        atBrokerColorFilter.StartInfo.FileName = "atbroker.exe";
        atBrokerColorFilter.StartInfo.Arguments = $"/colorfiltershortcut /resettransferkeys";
        atBrokerColorFilter.StartInfo.UseShellExecute = false;
        atBrokerColorFilter.StartInfo.CreateNoWindow = true;
        atBrokerColorFilter.Start();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected override void HandleSwitch(SwitchEventArgs e)
    {
        bool oldTheme = currentColorFilterActive;
        try
        {
            RegistryHandler.ColorFilterSetup();
            if (e.Theme == Theme.Dark)
            {
                RegistryHandler.SetColorFilter(true);
                LaunchAtBroker();
                currentColorFilterActive = true;

            }
            else
            {
                RegistryHandler.SetColorFilter(false);
                LaunchAtBroker();
                currentColorFilterActive = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not enable color filter:");
        }
        Logger.Info($"update info - previous: {oldTheme}, now: {currentColorFilterActive}, enabled: {Settings.Enabled}");
    }
}
