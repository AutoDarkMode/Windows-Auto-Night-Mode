using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.UI;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class WallpaperSwitchThemeFile : BaseComponent<WallpaperSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override int PriorityToLight => 25;
        public override int PriorityToDark => 25;
        private Theme currentIndividualTheme = Theme.Unknown;
        private Theme currentGlobalTheme = Theme.Unknown;
        private Theme currentSolidColorTheme = Theme.Unknown;
        private WallpaperPosition currentWallpaperPosition;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (newTheme == Theme.Dark)
            {
                return TypeNeedsUpdate(Settings.Component.TypeDark, Theme.Dark);
            }
            else if (newTheme == Theme.Light)
            {
                return TypeNeedsUpdate(Settings.Component.TypeLight, Theme.Light);
            }
            else if (currentWallpaperPosition != Settings.Component.Position)
            {
                return true;
            }
            return false;
        }

        private bool TypeNeedsUpdate(WallpaperType type, Theme targetTheme)
        {
            // if all wallpaper mode is selected and one needs an update, component also does.
            if (type == WallpaperType.All && (currentGlobalTheme != targetTheme || currentIndividualTheme != targetTheme))
            {
                return true;
            }
            else if (type == WallpaperType.Individual && currentIndividualTheme != targetTheme)
            {
                return true;
            }
            else if (type == WallpaperType.SolidColor && currentSolidColorTheme != targetTheme)
            {
                return true;
            }
            else if (type == WallpaperType.Global && currentGlobalTheme != targetTheme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme, SwitchEventArgs e)
        {
            // todo change behavior for win11 22H2, write and apply custom theme file. Use Winforms Screens to assing correct monitors.
            string oldIndividual = Enum.GetName(typeof(Theme), currentIndividualTheme);
            string oldGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme);
            string oldSolid = Enum.GetName(typeof(Theme), currentSolidColorTheme);

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
                LogHandleSwitch(Settings.Component.TypeDark, oldGlobal, oldIndividual, oldSolid, oldPos);
            }
            else if (newTheme == Theme.Light)
            {
                LogHandleSwitch(Settings.Component.TypeLight, oldGlobal, oldIndividual, oldSolid, oldPos);
            }
        }

        private void LogHandleSwitch(WallpaperType type, string oldGlobal, string oldIndividual, string oldSolid, string oldPos)
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
        }

        /// <summary>
        /// Handles the switch for each of the available wallpaper type modes.
        /// </summary>
        /// <param name="type">The wallpaper type that is selected</param>
        /// <param name="newTheme">The new theme that is targeted to be set</param>
        private void HandleSwitchByType(WallpaperType type, Theme newTheme)
        {
            if (type == WallpaperType.Individual)
            {
                WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);
                currentIndividualTheme = newTheme;
                currentGlobalTheme = Theme.Unknown;
                currentSolidColorTheme = Theme.Unknown;
            }
            else if (type == WallpaperType.Global)
            {
                WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
                currentGlobalTheme = newTheme;
                currentIndividualTheme = Theme.Unknown;
                currentSolidColorTheme = Theme.Unknown;

            }
            else if (type == WallpaperType.All)
            {
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
            }
            else if (type == WallpaperType.SolidColor)
            {
                WallpaperHandler.SetSolidColor(Settings.Component.SolidColors, newTheme);
                currentSolidColorTheme = newTheme;
                currentGlobalTheme = Theme.Unknown;
                currentIndividualTheme = Theme.Unknown;
            }
            try
            {
                File.Copy(Extensions.CustomThemePath, Path.Combine(Extensions.ThemeFolderPath, "Custom.theme"), true);
                ThemeHandler.Apply(Path.Combine(Extensions.ThemeFolderPath, "Custom.theme"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't refresh custom theme, wallpapers may desync");
            }
            ThemeFile temp = new(RegistryHandler.GetActiveThemePath());
            temp.Load();
            GlobalState.ManagedThemeFile.Desktop = temp.Desktop;
            GlobalState.ManagedThemeFile.Colors = temp.Colors;
        }

        /// <summary>
        /// This module needs its componentstate fetched from the win32 api to correctly function after a settings update
        /// </summary>
        /// <param name="newSettings"></param>
        public override void UpdateSettingsState(object newSettings)
        {

            bool isInit = Settings == null;
            base.UpdateSettingsState(newSettings);
            if (isInit) return;
            UpdateCurrentComponentState();
        }

        private void StateUpdateOnTypeToggle(WallpaperType current)
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
        }

        private void UpdateCurrentComponentState(bool isInitializing = false)
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

        public override void EnableHook()
        {
            DisplayHandler.DetectMonitors();
            currentWallpaperPosition = WallpaperHandler.GetPosition();
            base.EnableHook();
        }

        public override void DisableHook()
        {
            currentSolidColorTheme = Theme.Unknown;
            currentGlobalTheme = Theme.Unknown;
            base.DisableHook();
        }

        private Theme GetIndividualWallpapersState()
        {
            List<Tuple<string, string>> wallpapers = WallpaperHandler.GetWallpapers();
            List<Theme> wallpaperStates = new();
            // collect the wallpaper states of all wallpapers in the system
            foreach (Tuple<string, string> wallpaperInfo in wallpapers)
            {
                MonitorSettings settings = Settings.Component.Monitors.Find(m => m.Id == wallpaperInfo.Item1);
                if (settings != null)
                {
                    if (wallpaperInfo.Item2.ToLower().Equals(settings.DarkThemeWallpaper.ToLower()))
                    {
                        wallpaperStates.Add(Theme.Dark);
                    }
                    else if (wallpaperInfo.Item2.ToLower().Equals(settings.LightThemeWallpaper.ToLower()))
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
