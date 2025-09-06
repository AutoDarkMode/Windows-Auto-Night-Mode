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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class WallpaperSwitchThemeFile : WallpaperSwitch
{
    protected override void SwitchSolidColor(Theme newTheme)
    {
        if (newTheme == Theme.Dark)
        {
            GlobalState.ManagedThemeFile.Colors.Background = (WallpaperHandler.HexToRgb(Settings.Component.SolidColors.Dark),
                GlobalState.ManagedThemeFile.Colors.Background.Item2);
        }
        else
        {
            GlobalState.ManagedThemeFile.Colors.Background = (WallpaperHandler.HexToRgb(Settings.Component.SolidColors.Light),
                GlobalState.ManagedThemeFile.Colors.Background.Item2);
        }
        GlobalState.ManagedThemeFile.Desktop.Wallpaper = "";
        GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds = 0;
        GlobalState.ManagedThemeFile.Desktop.WindowsSpotlight = 0;
        GlobalState.ManagedThemeFile.Slideshow.Enabled = false;

        // WallpaperHandler.SetSolidColor(Settings.Component.SolidColors, newTheme);
        currentSolidColorTheme = newTheme;
        currentGlobalTheme = Theme.Unknown;
        currentIndividualTheme = Theme.Unknown;
        spotlightEnabled = false;
    }

    protected override bool SolidColorNeedsUpdateHandler()
    {
        HookPosition = HookPosition.PostSync;
        return true;
    }

    protected override void SwitchIndividual(Theme newTheme)
    {
        WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);

        if (currentSolidColorTheme != Theme.Unknown)
        {
            Logger.Debug("waiting for solid color to disable");
            Thread.Sleep(100);
        }
        currentIndividualTheme = newTheme;
        currentGlobalTheme = Theme.Unknown;
        currentSolidColorTheme = Theme.Unknown;
        spotlightEnabled = false;
    }


    protected override bool VerifyOperationIntegrity(SwitchEventArgs e)
    {
        var wantedAgreement = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result.Count;
        var wallpapersInThemeFile = GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers
            .Select(w => Path.GetFileName(w.Item1))
            .ToList();

        WallpaperType type = e.Theme == Theme.Dark ? Settings.Component.TypeDark : Settings.Component.TypeLight;

        switch (type)
        {
            case WallpaperType.Individual:
                List<string> wallpapersTarget = [.. Settings.Component.Monitors.
                    Select(m => Path.GetFileName(e.Theme == Theme.Dark ? m.DarkThemeWallpaper : m.LightThemeWallpaper))];
                return CheckAgreementIndividual(wallpapersInThemeFile, wallpapersTarget);
            case WallpaperType.Global:
                return CheckAgreementGlobal();
        }

        return true;
    }

    private bool CheckAgreementGlobal()
    {
        bool ok = Path.GetFileName(GlobalState.ManagedThemeFile.Desktop.Wallpaper) == Path.GetFileName(WallpaperHandler.GetGlobalWallpaper());
        if (ok)
        {
            Logger.Info($"wallpaper synchronization integrity check passed");
        }
        else
        {
            Logger.Warn($"wallpaper synchronization integrity check failed: wanted {GlobalState.ManagedThemeFile.Desktop.Wallpaper}, is {WallpaperHandler.GetGlobalWallpaper()}");
        }
        return ok;
    }

    private bool CheckAgreementIndividual(List<string> wallpapersInThemeFile, List<string> wallpapersTarget)
    {
        var wantedAgreement = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result.Count;

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in wallpapersInThemeFile)
        {
            counts.TryGetValue(name, out var c);
            counts[name] = c + 1;
        }

        int agreement = 0;
        foreach (var name in wallpapersTarget)
        {
            if (counts.TryGetValue(name, out var c) && c > 0)
            {
                agreement++;
                counts[name] = c - 1;
            }
        }

        bool ok = agreement == wantedAgreement;

        if (ok)
            Logger.Info($"wallpaper synchronization integrity check passed ({agreement}/{wantedAgreement})");
        else
            Logger.Warn($"wallpaper synchronization integrity check failed ({agreement}/{wantedAgreement})");

        return ok;
    }
}