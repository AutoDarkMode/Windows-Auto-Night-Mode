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

    protected override void SwitchGlobal(Theme newTheme)
    {
        WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
        if (newTheme == Theme.Light)
        {
            GlobalState.ManagedThemeFile.Desktop.Wallpaper = Settings.Component.GlobalWallpaper.Light;
        }
        else
        {
            GlobalState.ManagedThemeFile.Desktop.Wallpaper = Settings.Component.GlobalWallpaper.Dark;
        }
        GlobalState.ManagedThemeFile.Slideshow.Enabled = false;
        GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds = 0;
        currentGlobalTheme = newTheme;
        currentIndividualTheme = Theme.Unknown;
        currentSolidColorTheme = Theme.Unknown;
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

        if (e.Theme == Theme.Dark && Settings.Component.TypeDark == WallpaperType.Individual)
        {
            var wallpapersTarget = Settings.Component.Monitors
                .Select(m => Path.GetFileName(m.DarkThemeWallpaper))
                .ToList();
            return CheckAgreement(wallpapersInThemeFile, wallpapersTarget);
        }
        else if (e.Theme == Theme.Light && Settings.Component.TypeLight == WallpaperType.Individual)
        {
            var wallpapersTarget = Settings.Component.Monitors
                .Select(m => Path.GetFileName(m.LightThemeWallpaper))
                .ToList();
            return CheckAgreement(wallpapersInThemeFile, wallpapersTarget);
        }

        return true;
    }

    private bool CheckAgreement(List<string> wallpapersInThemeFile, List<string> wallpapersTarget)
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