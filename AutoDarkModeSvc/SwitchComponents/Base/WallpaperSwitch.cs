using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class WallpaperSwitch : BaseComponent<WallpaperSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override int PriorityToLight => 25;
        public override int PriorityToDark => 25;
        private Theme currentComponentTheme = Theme.Undefined;
        private WallpaperPosition currentWallpaperPosition;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (Settings.Component.Mode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.LightOnly && currentComponentTheme != Theme.Light)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch && currentComponentTheme != newTheme)
            {
                return true;
            }
            else if (currentWallpaperPosition != Settings.Component.Position)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            string oldPos = Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition);
            try
            {
                WallpaperHandler.SetWallpapers(Settings.Component.Monitors, Settings.Component.Position, newTheme);
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    currentComponentTheme = Theme.Dark;
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    currentComponentTheme = Theme.Light;
                }
                else
                {
                    currentComponentTheme = newTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set wallpapers");
            }
            Logger.Info($"update info - previous: {oldTheme}/{oldPos}, " +
                $"current: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{Enum.GetName(typeof(WallpaperPosition), currentWallpaperPosition)}, " +
                $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}/{Enum.GetName(typeof(WallpaperPosition), Settings.Component.Position)}");
        }

        /// <summary>
        /// This module needs its componentstate fetched from the win32 api to correctly function after a settings update
        /// </summary>
        /// <param name="newSettings"></param>
        public override void UpdateSettingsState(object newSettings)
        {
            base.UpdateSettingsState(newSettings);
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
                currentComponentTheme = Theme.Dark;
            }
            else if (wallpaperStates.TrueForAll(c => c == Theme.Light))
            {
                currentComponentTheme = Theme.Light;
            }
            else
            {
                currentComponentTheme = Theme.Undefined;
            }
            currentWallpaperPosition = WallpaperHandler.GetPosition();
        }


    }
}
