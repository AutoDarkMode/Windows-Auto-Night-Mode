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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.UserDataAccounts.SystemAccess;
using Windows.UI;
using Windows.UI.Composition;
using static AutoDarkModeSvc.Handlers.WallpaperHandler;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class WallpaperSwitch : BaseComponent<WallpaperSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override int PriorityToLight => 25;
        public override int PriorityToDark => 25;
        public override HookPosition HookPosition { get; protected set; } = HookPosition.PreSync;
        protected Theme currentIndividualTheme = Theme.Unknown;
        protected Theme currentGlobalTheme = Theme.Unknown;
        protected Theme currentSolidColorTheme = Theme.Unknown;
        protected bool? spotlightEnabled = null;
        protected WallpaperPosition currentWallpaperPosition;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (currentWallpaperPosition != Settings.Component.Position)
            {
                return true;
            }
            if (e.Theme == Theme.Dark)
            {
                return TypeNeedsUpdate(Settings.Component.TypeDark, Theme.Dark);
            }
            else if (e.Theme == Theme.Light)
            {
                return TypeNeedsUpdate(Settings.Component.TypeLight, Theme.Light);
            }
            
            return false;
        }

        protected bool TypeNeedsUpdate(WallpaperType type, Theme targetTheme)
        {
            // if all wallpaper mode is selected and one needs an update, component also does.
            if (type == WallpaperType.All && (currentGlobalTheme != targetTheme || currentIndividualTheme != targetTheme))
            {
                HookPosition = HookPosition.PreSync;
                return true;
            }
            else if (type == WallpaperType.Individual && currentIndividualTheme != targetTheme)
            {
                HookPosition = HookPosition.PreSync;
                return true;
            }
            else if (type == WallpaperType.SolidColor && currentSolidColorTheme != targetTheme)
            {
                return SolidColorNeedsUpdateHandler();
            }
            else if (type == WallpaperType.Global && currentGlobalTheme != targetTheme)
            {
                HookPosition = HookPosition.PostSync;
                return true;
            }
            else if (type == WallpaperType.Spotlight)
            {
                if (spotlightEnabled.HasValue && spotlightEnabled.Value)
                {
                    return false;
                }
                HookPosition = HookPosition.PostSync;
                return true;
            }
            return false;
        }

        protected virtual bool SolidColorNeedsUpdateHandler()
        {
            HookPosition = HookPosition.PreSync;
            return true;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            Theme newTheme = e.Theme;
            // todo change behavior for win11 22H2, write and apply custom theme file. Use Winforms Screens to assing correct monitors.
            string oldIndividual = Enum.GetName(typeof(Theme), currentIndividualTheme);
            string oldGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme);
            string oldSolid = Enum.GetName(typeof(Theme), currentSolidColorTheme);
            string oldSpotlight = spotlightEnabled.HasValue ? spotlightEnabled.Value.ToString().ToLower() : "unknown";

            string oldPos = Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition);
            try
            {
                if (newTheme == Theme.Dark)
                {
                    HandleSwitchByType(Settings.Component.TypeDark, Theme.Dark);
                }
                else if (newTheme == Theme.Light)
                {
                    HandleSwitchByType(Settings.Component.TypeLight, Theme.Light);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set wallpapers");
            }

            if (newTheme == Theme.Dark)
            {
                LogHandleSwitch(Settings.Component.TypeDark, oldGlobal, oldIndividual, oldSolid, oldPos, oldSpotlight);
            }
            else if (newTheme == Theme.Light)
            {
                LogHandleSwitch(Settings.Component.TypeLight, oldGlobal, oldIndividual, oldSolid, oldPos, oldSpotlight);
            }
        }

        protected void LogHandleSwitch(WallpaperType type, string oldGlobal, string oldIndividual, string oldSolid, string oldPos, string oldSpotlight)
        {
            if (type == WallpaperType.All)
            {
                string currentIndividual = Enum.GetName(typeof(Theme), currentIndividualTheme);
                string currentGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme);
                Logger.Info($"update info - previous global: {oldGlobal}/{oldPos}, " +
                            $"global now: {currentGlobal}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
                Logger.Info($"update info - previous individual: {oldIndividual}/{oldPos}, " +
                            $"individual now: {currentIndividual}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.Individual)
            {
                string currentIndividual = Enum.GetName(typeof(Theme), currentIndividualTheme);
                Logger.Info($"update info - previous: {oldIndividual}/{oldPos}, " +
                            $"now: {currentIndividual}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.Global)
            {
                string currentGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme);
                Logger.Info($"update info - previous: {oldGlobal}/{oldPos}, " +
                            $"now: {currentGlobal}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.SolidColor)
            {
                string currentSolid = Enum.GetName(typeof(Theme), currentSolidColorTheme);
                Logger.Info($"update info - previous: {oldSolid}/{oldPos}, " +
                            $"now: {currentSolid}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.Spotlight)
            {
                Logger.Info($"update info - previous: {oldSpotlight}, " +
                            $"now: {(spotlightEnabled.HasValue ? spotlightEnabled.Value.ToString().ToLower() : "unknown")}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
        }

        /// <summary>
        /// Handles the switch for each of the available wallpaper type modes.
        /// </summary>
        /// <param name="type">The wallpaper type that is selected</param>
        /// <param name="newTheme">The new theme that is targeted to be set</param>
        protected void HandleSwitchByType(WallpaperType type, Theme newTheme)
        {
            if (type == WallpaperType.Individual)
            {
                SwitchIndividual(newTheme);
            }
            else if (type == WallpaperType.Global)
            {
                SwitchGlobal(newTheme);
            }
            else if (type == WallpaperType.All)
            {
                Logger.Error("not implemented anymore");
                /*
                bool globalSwitched = false;
                if (currentGlobalTheme != newTheme)
                {
                    WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
                    globalSwitched = true;
                }
                if (currentIndividualTheme != newTheme || globalSwitched)
                {
                    WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);
                }
                currentGlobalTheme = newTheme;
                currentIndividualTheme = newTheme;
                currentSolidColorTheme = Theme.Unknown;
                spotlightEnabled = false;
                */
            }
            else if (type == WallpaperType.SolidColor)
            {
                SwitchSolidColor(newTheme);
            }
            else if (type == WallpaperType.Spotlight)
            {
                GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds = 0;
                GlobalState.ManagedThemeFile.Desktop.WindowsSpotlight = 1;
                GlobalState.ManagedThemeFile.Desktop.Wallpaper = @"%SystemRoot%\web\wallpaper\spotlight\img50.jpg";
                currentSolidColorTheme = Theme.Unknown;
                currentGlobalTheme = Theme.Unknown;
                currentIndividualTheme = Theme.Unknown;
                spotlightEnabled = true;
            }
        }

        protected virtual void SwitchGlobal(Theme newTheme)
        {
            WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
            currentGlobalTheme = newTheme;
            currentIndividualTheme = Theme.Unknown;
            currentSolidColorTheme = Theme.Unknown;
            spotlightEnabled = false;
        } 

        protected virtual void SwitchIndividual(Theme newTheme)
        {
            WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);
            currentIndividualTheme = newTheme;
            currentGlobalTheme = Theme.Unknown;
            currentSolidColorTheme = Theme.Unknown;
            spotlightEnabled = false;
        }

        protected virtual void SwitchSolidColor(Theme newTheme)
        {
            WallpaperHandler.SetSolidColor(Settings.Component.SolidColors, newTheme);
            currentSolidColorTheme = newTheme;
            currentGlobalTheme = Theme.Unknown;
            currentIndividualTheme = Theme.Unknown;
            spotlightEnabled = false;
        }

        /// <summary>
        /// This module needs its componentstate fetched from the win32 api to correctly function after a settings update
        /// </summary>
        protected override void UpdateSettingsState()
        {
            UpdateCurrentComponentState();
        }

        protected void StateUpdateOnTypeToggle(WallpaperType current)
        {
            if (current == WallpaperType.All)
            {
                currentGlobalTheme = Theme.Unknown;
            }
            else if (current == WallpaperType.Global)
            {
                currentGlobalTheme = Theme.Unknown;
            }
            else if (current == WallpaperType.SolidColor)
            {
                currentSolidColorTheme = Theme.Unknown;
            }
            else if (spotlightEnabled.HasValue)
            {
                spotlightEnabled = null;
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

            // additinoally, if the user has changed the type dark before, an update is also required
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
            string globalWallpaper = WallpaperHandler.GetGlobalWallpaper();
            if (globalWallpaper == Settings.Component.GlobalWallpaper.Light) currentGlobalTheme = Theme.Light;
            else if (globalWallpaper == Settings.Component.GlobalWallpaper.Dark) currentGlobalTheme = Theme.Dark;

                        // solid color enable state synchronization
            if (GlobalState.ManagedThemeFile.Desktop.Wallpaper.Length == 0 && GlobalState.ManagedThemeFile.Desktop.MultimonBackgrounds == 0)
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

        protected Theme GetIndividualWallpapersState()
        {
            // We no longer use this because it returns disconnected displays
            // List<Tuple<string, string>> wallpapers = WallpaperHandler.GetWallpapers();
            var monitors = Task.Run(DisplayHandler.GetMonitorInfosAsync).Result;
            List<Theme> wallpaperStates = new();
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

    }
}
