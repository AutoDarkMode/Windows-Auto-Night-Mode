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
using static AutoDarkModeSvc.Handlers.WallpaperHandler;

namespace AutoDarkModeSvc.SwitchComponents.Base;

internal class WallpaperSwitch : BaseComponent<WallpaperSwitchSettings>
{
    // ### Component metadata and state ###

    public override bool ThemeHandlerCompatibility => false;
    public override int PriorityToLight => 25;
    public override int PriorityToDark => 25;
    public override HookPosition HookPosition { get; protected set; } = HookPosition.PreSync;
    protected Theme currentIndividualTheme = Theme.Unknown;
    protected Theme currentGlobalTheme = Theme.Unknown;
    protected Theme currentSolidColorTheme = Theme.Unknown;
    protected bool? spotlightEnabled = null;
    protected WallpaperPosition currentWallpaperPosition;

    protected bool IsSpotlightCompatible()
    {
        if (Environment.OSVersion.Version.Build == (int)WindowsBuilds.Win11_22H2)
        {
            bool hasUbr = int.TryParse(RegistryHandler.GetUbr(), out int ubr);
            if (hasUbr && ubr < (int)WindowsBuildsUbr.Win11_22H2_Spotlight)
            {
                Logger.Warn($"spotlight not supported on build {(int)WindowsBuildsUbr.Win11_22H2_Spotlight}.{ubr}");
                return false;
            }
        }
        else if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.Win11_22H2)
        {
            if (Environment.OSVersion.Version.Build == (int)WindowsBuilds.Win10_22H2)
            {
                bool hasUbr = int.TryParse(RegistryHandler.GetUbr(), out int ubr);
                if (hasUbr)
                {
                    if (ubr >= (int)WindowsBuildsUbr.Win10_22H2_Spotlight)
                    {
                        return true;
                    }
                    Logger.Warn($"spotlight not supported on build {Environment.OSVersion.Version.Build}.{ubr}");
                    return false;
                }
            }
            Logger.Warn($"spotlight not supported on build {Environment.OSVersion.Version.Build}");
            return false;
        }

        return true;
    }

    // ### Switch decision and execution ###

    protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
    {
        if (currentWallpaperPosition != Settings.Component.Position) return true;

        return e.Theme switch
        {
            Theme.Light => TypeNeedsUpdate(Settings.Component.TypeLight, Theme.Light),
            Theme.Dark => TypeNeedsUpdate(Settings.Component.TypeDark, Theme.Dark),
            _ => false,
        };
    }

    protected bool TypeNeedsUpdate(WallpaperType type, Theme targetTheme)
    {
        // if all wallpaper mode is selected and one needs an update, component also does.
        switch (type)
        {
            case WallpaperType.Individual when currentIndividualTheme != targetTheme:
                HookPosition = HookPosition.PreSync;
                return true;
            case WallpaperType.SolidColor when currentSolidColorTheme != targetTheme:
                HookPosition = HookPosition.PostSync;
                return true;
            case WallpaperType.Global when currentGlobalTheme != targetTheme:
                HookPosition = HookPosition.PostSync;
                return true;
            case WallpaperType.Spotlight when IsSpotlightCompatible():
                HookPosition = HookPosition.PostSync;
                if (spotlightEnabled.HasValue && spotlightEnabled.Value)
                {
                    return false;
                }

                return true;
        }

        return false;
    }

    protected override void HandleSwitch(SwitchEventArgs e)
    {
        Theme newTheme = e.Theme;
        // TODO: change behavior for win11 22H2, write and apply custom theme file. Use WinForms Screens to assign correct monitors.
        string oldIndividual = Enum.GetName(currentIndividualTheme);
        string oldGlobal = Enum.GetName(currentGlobalTheme);
        string oldSolid = Enum.GetName(currentSolidColorTheme);
        string oldSpotlight = spotlightEnabled.HasValue ? spotlightEnabled.Value.ToString().ToLower() : "unknown";

        string oldPos = Enum.GetName(currentWallpaperPosition);
        try
        {
            switch (newTheme)
            {
                case Theme.Light:
                    HandleSwitchByType(Settings.Component.TypeLight, Theme.Light);
                    break;
                case Theme.Dark:
                    HandleSwitchByType(Settings.Component.TypeDark, Theme.Dark);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not set wallpapers");
        }

        switch (newTheme)
        {
            case Theme.Light:
                LogHandleSwitch(Settings.Component.TypeLight, oldGlobal, oldIndividual, oldSolid, oldPos, oldSpotlight);
                break;
            case Theme.Dark:
                LogHandleSwitch(Settings.Component.TypeDark, oldGlobal, oldIndividual, oldSolid, oldPos, oldSpotlight);
                break;
        }
    }

    protected void LogHandleSwitch(WallpaperType type, string oldGlobal, string oldIndividual, string oldSolid,
        string oldPos, string oldSpotlight)
    {
        switch (type)
        {
            case WallpaperType.Individual:
            {
                var currentIndividual = Enum.GetName(currentIndividualTheme);
                Logger.Info($"update info - previous: {oldIndividual}/{oldPos}, " +
                            $"now: {currentIndividual}/{Enum.GetName(currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(type)}");
                break;
            }

            case WallpaperType.Global:
            {
                var currentGlobal = Enum.GetName(currentGlobalTheme);
                Logger.Info($"update info - previous: {oldGlobal}/{oldPos}, " +
                            $"now: {currentGlobal}/{Enum.GetName(currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(type)}");
                break;
            }

            case WallpaperType.SolidColor:
            {
                var currentSolid = Enum.GetName(currentSolidColorTheme);
                Logger.Info($"update info - previous: {oldSolid}/{oldPos}, " +
                            $"now: {currentSolid}/{Enum.GetName(currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(type)}");
                break;
            }

            case WallpaperType.Spotlight:
                Logger.Info($"update info - previous: {oldSpotlight}, " +
                                    $"now: {(spotlightEnabled.HasValue ? spotlightEnabled.Value.ToString().ToLower() : "unknown")}, " +
                                    $"type: {Enum.GetName(type)}");
                break;
        }
    }

    // ### Wallpaper type handlers ###

    /// <summary>
    /// Handles the switch for each of the available wallpaper type modes.
    /// </summary>
    /// <param name="type">The wallpaper type that is selected</param>
    /// <param name="newTheme">The new theme that is targeted to be set</param>
    protected void HandleSwitchByType(WallpaperType type, Theme newTheme)
    {
        switch (type)
        {
            case WallpaperType.Individual:
                SwitchIndividual(newTheme);
                break;
            case WallpaperType.Global:
                SwitchGlobal(newTheme);
                break;
            case WallpaperType.SolidColor:
                SwitchSolidColor(newTheme);
                break;
            case WallpaperType.Spotlight when IsSpotlightCompatible():
                GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds = 0;
                GlobalState.ManagedThemeFile.Desktop.WindowsSpotlight = 1;
                GlobalState.ManagedThemeFile.Desktop.Wallpaper = @"%SystemRoot%\web\wallpaper\spotlight\img50.jpg";
                currentSolidColorTheme = Theme.Unknown;
                currentGlobalTheme = Theme.Unknown;
                currentIndividualTheme = Theme.Unknown;
                spotlightEnabled = true;
                break;
        }
    }

    protected virtual void SwitchGlobal(Theme newTheme)
    {
        (bool ok, string wallpaper) = WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
        if (ok)
        {
            GlobalState.ManagedThemeFile.Desktop.Wallpaper = wallpaper;
            GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds = 0;
            GlobalState.ManagedThemeFile.Desktop.WindowsSpotlight = 0;
        }

        currentGlobalTheme = newTheme;
        currentIndividualTheme = Theme.Unknown;
        currentSolidColorTheme = Theme.Unknown;
        spotlightEnabled = false;
    }

    protected void SwitchIndividual(Theme newTheme)
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

    protected void SwitchSolidColor(Theme newTheme)
    {
        GlobalState.ManagedThemeFile.Colors.Background = newTheme switch
        {
            Theme.Dark => (HexToRgb(Settings.Component.SolidColors.Dark), GlobalState.ManagedThemeFile.Colors.Background.Item2),
            _ => (HexToRgb(Settings.Component.SolidColors.Light), GlobalState.ManagedThemeFile.Colors.Background.Item2),
        };

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


    // ### State synchronization ###

    /// <summary>
    /// This module needs its component state fetched from the win32 api to correctly function after a settings update
    /// </summary>
    protected override void UpdateSettingsState()
    {
        UpdateCurrentComponentState();
    }

    protected void StateUpdateOnTypeToggle(WallpaperType current)
    {
        switch (current)
        {
            case WallpaperType.Global:
                currentGlobalTheme = Theme.Unknown;
                break;
            case WallpaperType.SolidColor:
                currentSolidColorTheme = Theme.Unknown;
                break;
            default:
                if (spotlightEnabled.HasValue) spotlightEnabled = null;
                break;
        }
    }

    protected void UpdateCurrentComponentState(bool isInitializing = false)
    {
        if (Settings == null || SettingsBefore == null || (!Initialized && !isInitializing))
        {
            return;
        }

        string globalLightBefore = SettingsBefore.Component.GlobalWallpaper.Light ?? "";
        string globalDarkBefore = SettingsBefore.Component.GlobalWallpaper.Dark ?? "";
        string globalLightAfter = Settings.Component.GlobalWallpaper.Light ?? "";
        string globalDarkAfter = Settings.Component.GlobalWallpaper.Dark ?? "";

        // check if the global wallpaper section has been modified.
        // Since we don't have target theme information here, if one value changes, we want a theme refresh
        if (!globalDarkBefore.Equals(globalDarkAfter))
        {
            currentGlobalTheme = Theme.Unknown;
        }

        if (!globalLightBefore.Equals(globalLightAfter))
        {
            currentGlobalTheme = Theme.Unknown;
        }

        // Same behavior with solid color
        if (!SettingsBefore.Component.SolidColors.Light.Equals(Settings.Component.SolidColors.Light))
        {
            currentSolidColorTheme = Theme.Unknown;
        }

        if (!SettingsBefore.Component.SolidColors.Dark.Equals(Settings.Component.SolidColors.Dark))
        {
            currentSolidColorTheme = Theme.Unknown;
        }

        // additionally, if the user has changed the type dark before, an update is also required
        if (SettingsBefore.Component.TypeDark != Settings.Component.TypeDark)
        {
            StateUpdateOnTypeToggle(Settings.Component.TypeDark);
        }

        if (SettingsBefore.Component.TypeLight != Settings.Component.TypeLight)
        {
            StateUpdateOnTypeToggle(Settings.Component.TypeLight);
        }

        currentIndividualTheme = GetIndividualWallpapersState();
        currentWallpaperPosition = WallpaperHandler.GetPosition();
    }

    protected override void EnableHook()
    {
        currentWallpaperPosition = WallpaperHandler.GetPosition();
        currentIndividualTheme = GetIndividualWallpapersState();

        // force spotlight state to null
        // todo maybe do some kind of detection beforehand,
        // but might prove difficult because the managed theme is not necessarily the applied theme
        // and theme state tracking is only done with unmanaged themes for disk access reasons.
        spotlightEnabled = null;

        // global wallpaper enable state synchronization;
        string globalWallpaper = WallpaperHandler.GetGlobalWallpaper().ToLower();
        if (globalWallpaper == Settings.Component.GlobalWallpaper.Light?.ToLower()) currentGlobalTheme = Theme.Light;
        else if (globalWallpaper == Settings.Component.GlobalWallpaper.Dark?.ToLower()) currentGlobalTheme = Theme.Dark;

        // solid color enable state synchronization
        if (GlobalState.ManagedThemeFile.Desktop.Wallpaper.Length == 0 &&
            GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds == 0)
        {
            string solidColorHex = WallpaperHandler.GetSolidColor();
            if (solidColorHex == Settings.Component.SolidColors.Light) currentSolidColorTheme = Theme.Light;
            else if (solidColorHex == Settings.Component.SolidColors.Dark) currentSolidColorTheme = Theme.Dark;
        }
    }

    protected override void DisableHook()
    {
        currentSolidColorTheme = Theme.Unknown;
        currentGlobalTheme = Theme.Unknown;
        spotlightEnabled = null;
    }

    // ### Verification and callbacks ###

    protected override void Callback(SwitchEventArgs e)
    {
        if (spotlightEnabled.GetValueOrDefault(false)) RegistryHandler.SetSpotlightState(true);
        WallpaperType type = e.Theme == Theme.Dark ? Settings.Component.TypeDark : Settings.Component.TypeLight;

        if (type == WallpaperType.Spotlight)
        {
            Logger.Debug("waiting 4 seconds for spotlight to apply");
            Thread.Sleep(4000);
        }
    }

    protected override bool VerifyOperationIntegrity(SwitchEventArgs e)
    {
        var currentMonitors = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result;
        int currentMonitorCount = currentMonitors.Count;

        WallpaperType type = e.Theme == Theme.Dark ? Settings.Component.TypeDark : Settings.Component.TypeLight;

        switch (type)
        {
            case WallpaperType.Individual:
                List<string> wallpapersTarget =
                [
                    ..Settings.Component.Monitors
                        .Where(m => File.Exists(e.Theme == Theme.Dark ? m.DarkThemeWallpaper: m.LightThemeWallpaper))
                        .Where(m => currentMonitors.Select(c => c.DeviceId).ToList().Contains(m.Id))
                        .Select(m => Path.GetFileName(e.Theme == Theme.Dark ? m.DarkThemeWallpaper : m.LightThemeWallpaper)
                        .ToLower())
                ];

                if (wallpapersTarget.Count == 0)
                {
                    Logger.Warn("wallpaper synchronization: no wallpapers selected");
                    return true;
                }

                var themeFileMonitorCount = GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers.Count;
                if (themeFileMonitorCount > currentMonitorCount)
                {
                    // if the managed theme file has more multi monitor wallpapers, then we truncate those extra monitors
                    // otherwise we will potentially lose synchronization state
                    GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                    GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers =
                        GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers.Where((a, b) => b < currentMonitorCount).ToList();
                    Logger.Warn($"managed theme file contained more wallpapers than there are monitors, pruned wallpaper list from {themeFileMonitorCount} to {currentMonitorCount}");
                }

                var wallpapersInThemeFile = GlobalState.ManagedThemeFile.Desktop.MultimonWallpapers
                    .Select(w => Path.GetFileName(w.Item1).ToLower())
                    .ToList();

                return CheckAgreementIndividual(wallpapersInThemeFile, wallpapersTarget);
        }
        return true;
    }

    // ### Runtime state detection ###

    protected Theme GetIndividualWallpapersState()
    {
        // We no longer use this because it returns disconnected displays
        // List<Tuple<string, string>> wallpapers = WallpaperHandler.GetWallpapers();
        var monitors = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result;
        List<Theme> wallpaperStates = [];
        IDesktopWallpaper handler = (IDesktopWallpaper)new DesktopWallpaperClass();
        // collect the wallpaper states of all wallpapers in the system
        foreach (var monitor in monitors)
        {
            string monitorId = monitor.DeviceId;
            if (monitorId == null)
            {
                wallpaperStates.Add(Theme.Unknown);
            }
            else
            {
                string wallpaper = handler.GetWallpaper(monitorId);
                MonitorSettings settings = Settings.Component.Monitors.Find(m => m.Id == monitorId);
                if (settings != null)
                {
                    if (wallpaper.ToLower().Equals(settings.DarkThemeWallpaper.ToLower()))
                    {
                        wallpaperStates.Add(Theme.Dark);
                    }
                    else if (wallpaper.ToLower().Equals(settings.LightThemeWallpaper.ToLower()))
                    {
                        wallpaperStates.Add(Theme.Light);
                    }
                    else
                    {
                        wallpaperStates.Add(Theme.Unknown);
                        break;
                    }
                }
                else
                {
                    wallpaperStates.Add(Theme.Unknown);
                    break;
                }
            }
        }

        // if one single wallpaper does not match a theme, then we don't know the state and it needs to be updated
        if (wallpaperStates.TrueForAll(c => c == Theme.Dark))
        {
            return Theme.Dark;
        }
        else if (wallpaperStates.TrueForAll(c => c == Theme.Light))
        {
            return Theme.Light;
        }
        else
        {
            return Theme.Unknown;
        }
    }

    // ### Validation helpers ###

    private bool CheckAgreementIndividual(List<string> wallpapersInThemeFile, List<string> wallpapersTarget)
    {
        // Count how many of each wallpaper exists in the lists
        var requiredCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var wallpaperName in wallpapersTarget)
        {
            requiredCounts.TryGetValue(wallpaperName, out var count);
            requiredCounts[wallpaperName] = count + 1;
        }

        var availableCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var wallpaperName in wallpapersInThemeFile)
        {
            availableCounts.TryGetValue(wallpaperName, out var count);
            availableCounts[wallpaperName] = count + 1;
        }

        int totalRequired = wallpapersTarget.Count;
        int matched = 0;

        foreach (var requiredEntry in requiredCounts)
        {
            string wallpaperName = requiredEntry.Key;
            int requiredCount = requiredEntry.Value;

            int availableCount = availableCounts.TryGetValue(wallpaperName, out var count) ? count : 0;
            matched += Math.Min(availableCount, requiredCount);

            if (requiredCount < availableCount)
            {
                int missing = requiredCount - availableCount;
                Logger.Warn($"wallpaper synchronization: maybe missing {wallpaperName} x{missing}");
            }
        }

        double coverage = (double)matched / totalRequired;
        bool ok = coverage >= 1;

        if (ok)
        {
            Logger.Info($"wallpaper synchronization: integrity check passed ({matched}/{totalRequired}, {coverage:P0} coverage)");
        }
        else
        {
            Logger.Warn($"wallpaper synchronization: integrity check failed ({matched}/{totalRequired}, {coverage:P0} coverage)");
            Logger.Warn($"wallpaper synchronization: required wallpaper list: [{string.Join(", ", wallpapersInThemeFile)}], target list: [{string.Join(", ", wallpapersTarget)}]");
        }

        return ok;
    }
}
