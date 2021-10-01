using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class WallpaperSwitch : BaseComponent<WallpaperSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override int PriorityToLight => 25;
        public override int PriorityToDark => 25;
        private Theme currentIndividualTheme = Theme.Undefined;
        private Theme currentGlobalTheme = Theme.Undefined;
        private Theme currentSolidColorTheme = Theme.Undefined;
        private WallpaperPosition currentWallpaperPosition;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {

            if (Settings.Component.Mode == Mode.DarkOnly && TypeNeedsUpdate(Settings.Component.TypeDark, Theme.Dark))
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.LightOnly && TypeNeedsUpdate(Settings.Component.TypeLight, Theme.Light))
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch)
            {
                if (newTheme == Theme.Dark)
                {
                    return TypeNeedsUpdate(Settings.Component.TypeDark, Theme.Dark);
                }
                else if (newTheme == Theme.Light)
                {
                    return TypeNeedsUpdate(Settings.Component.TypeLight, Theme.Light);
                }
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

        protected override void HandleSwitch(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), Theme.Undefined);

            string oldPos = Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition);
            try
            {
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    Theme before = HandleSwitchByType(Settings.Component.TypeDark, Theme.Dark);
                    oldTheme = Enum.GetName(typeof(Theme), before);
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    Theme before = HandleSwitchByType(Settings.Component.TypeLight, Theme.Light);
                    oldTheme = Enum.GetName(typeof(Theme), before);
                }
                else
                {
                    if (newTheme == Theme.Dark)
                    {
                        Theme before = HandleSwitchByType(Settings.Component.TypeDark, Theme.Dark);
                        oldTheme = Enum.GetName(typeof(Theme), before);
                    }
                    else if (newTheme == Theme.Light)
                    {
                        Theme before = HandleSwitchByType(Settings.Component.TypeLight, Theme.Light);
                        oldTheme = Enum.GetName(typeof(Theme), before);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set wallpapers");
            }

            if (newTheme == Theme.Dark)
            {
                LogHandleSwitch(Settings.Component.TypeDark, oldTheme, oldPos);
            }
            else if (newTheme == Theme.Light)
            {
                LogHandleSwitch(Settings.Component.TypeLight, oldTheme, oldPos);
            }
        }

        private void LogHandleSwitch(WallpaperType type, string oldTheme, string oldPos)
        {
            if (type == WallpaperType.All)
            {
                string currentGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme == currentIndividualTheme ? currentGlobalTheme : Theme.Undefined);
                Logger.Info($"update info - previous: {oldTheme}/{oldPos}, " +
                            $"current: {currentGlobal}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}/{Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.Individual)
            {
                string currentIndividual = Enum.GetName(typeof(Theme), currentIndividualTheme);
                Logger.Info($"update info - previous: {oldTheme}/{oldPos}, " +
                            $"current: {currentIndividual}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}/{Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.Global)
            {
                string currentGlobal = Enum.GetName(typeof(Theme), currentGlobalTheme);
                Logger.Info($"update info - previous: {oldTheme}/{oldPos}, " +
                            $"current: {currentGlobal}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}/{Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
            else if (type == WallpaperType.SolidColor)
            {
                string currentSolid = Enum.GetName(typeof(Theme), currentSolidColorTheme);
                Logger.Info($"update info - previous: {oldTheme}/{oldPos}, " +
                            $"current: {currentSolid}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                            $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}/{Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}, " +
                            $"type: {Enum.GetName(typeof(WallpaperType), type)}");
            }
        }

        private Theme HandleSwitchByType(WallpaperType type, Theme newTheme)
        {
            if (type == WallpaperType.Individual)
            {
                Theme before = currentIndividualTheme;
                WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);
                currentIndividualTheme = newTheme;
                currentGlobalTheme = Theme.Undefined;
                currentSolidColorTheme = Theme.Undefined;
                return before;
            }
            else if (type == WallpaperType.Global)
            {
                Theme before = currentGlobalTheme;
                WallpaperHandler.SetGlobalWallpaper(Settings.Component.GlobalWallpaper, newTheme);
                currentGlobalTheme = newTheme;
                currentIndividualTheme = Theme.Undefined;
                currentSolidColorTheme = Theme.Undefined;

                return before;
            }
            else if (type == WallpaperType.All)
            {
                Theme before = currentGlobalTheme == currentIndividualTheme ? currentGlobalTheme : Theme.Undefined;
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
                currentSolidColorTheme = Theme.Undefined;
                return before;
            }
            else if (type == WallpaperType.SolidColor)
            {
                Theme before = currentSolidColorTheme;
                WallpaperHandler.SetSolidColor(Settings.Component.SolidColors, newTheme);
                currentSolidColorTheme = newTheme;
                currentGlobalTheme = Theme.Undefined;
                currentIndividualTheme = Theme.Undefined;
                return before;
            }
            return Theme.Undefined;
        }

        /// <summary>
        /// This module needs its componentstate fetched from the win32 api to correctly function after a settings update
        /// </summary>
        /// <param name="newSettings"></param>
        public override void UpdateSettingsState(object newSettings)
        {
            string globalLightBefore = "";
            string globalDarkBefore = "";
            Color colorLightBefore = Color.FromArgb(0, 0, 0, 0);
            Color colorDarkBefore = Color.FromArgb(0, 0, 0, 0);
            if (Settings != null)
            {
                if (Settings.Component.GlobalWallpaper.Dark != null)
                {
                    globalDarkBefore = Settings.Component.GlobalWallpaper.Dark;
                }
                if (Settings.Component.GlobalWallpaper.Light != null)
                {
                    globalLightBefore = Settings.Component.GlobalWallpaper.Light;
                }
                colorLightBefore = Settings.Component.SolidColors.Light;
                colorDarkBefore = Settings.Component.SolidColors.Dark;
            }
            
            base.UpdateSettingsState(newSettings);

            if (Settings.Component.GlobalWallpaper.Dark == null || !globalDarkBefore.Equals(Settings.Component.GlobalWallpaper.Dark))
            {
                currentGlobalTheme = Theme.Undefined;
            }
            if (Settings.Component.GlobalWallpaper.Light == null || !globalLightBefore.Equals(Settings.Component.GlobalWallpaper.Light))
            {
                currentGlobalTheme = Theme.Undefined;
            }

            if (!colorLightBefore.Equals(Settings.Component.SolidColors.Light))
            {
                currentSolidColorTheme = Theme.Undefined;
            }

            if (!colorDarkBefore.Equals(Settings.Component.SolidColors.Dark))
            {
                currentSolidColorTheme = Theme.Undefined;
            }


            // rerun enable hook for wallpaper switch when cfg is updated
            // necessary to hot-reload wallpapers
            UpdateCurrentComponentState();
        }

        public override void EnableHook()
        {
            WallpaperHandler.DetectMonitors();
            UpdateCurrentComponentState();
            base.EnableHook();
        }

        private void UpdateCurrentComponentState()
        {
            bool all = Settings.Component.TypeDark == WallpaperType.All || Settings.Component.TypeLight == WallpaperType.All;
            bool individual = Settings.Component.TypeDark == WallpaperType.Individual || Settings.Component.TypeLight == WallpaperType.Individual;
            //bool global = Settings.Component.TypeDark == WallpaperType.Global || Settings.Component.TypeLight == WallpaperType.Global;
            //bool solid = Settings.Component.TypeDark == WallpaperType.SolidColor || Settings.Component.TypeLight == WallpaperType.SolidColor;

            //bool indivUpdated = false;
            if (individual || all)
            {
                currentIndividualTheme = GetIndividualWallpapersState();
                currentWallpaperPosition = WallpaperHandler.GetPosition();
            }
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
                        wallpaperStates.Add(Theme.Undefined);
                        break;
                    }
                }
                else
                {
                    wallpaperStates.Add(Theme.Undefined);
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
                return Theme.Undefined;
            }
        }

    }
}
