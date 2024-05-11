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
using Windows.UI;
using Windows.UI.Composition;
using static AutoDarkModeSvc.Handlers.WallpaperHandler;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
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
    }
}
