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
        private Theme currentComponentTheme = Theme.Undefined;

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
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
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
            Logger.Info($"update info - previous: {oldTheme}, current: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }

        public override void EnableHook()
        {
            WallpaperHandler.DetectMonitors();
            List<Tuple<string, string>> wallpapers = WallpaperHandler.GetWallpapers();
            List<Theme> wallpaperStates = new();
            // collect the wallpaper states of all wallpapers in the system
            foreach (Tuple<string, string> wallpaperInfo in wallpapers)
            {
                MonitorSettings settings = Settings.Component.Monitors.Find(m => m.Id == wallpaperInfo.Item1);
                if (settings != null)
                {
                    if (wallpaperInfo.Item2 == settings.DarkThemeWallpaper)
                    {
                        wallpaperStates.Add(Theme.Dark);
                    }
                    else if (wallpaperInfo.Item2 == settings.LightThemeWallpaper)
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
            base.EnableHook();
        }


    }
}
