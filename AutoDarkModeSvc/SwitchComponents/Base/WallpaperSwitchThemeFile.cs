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
using Windows.UI;
using Windows.UI.Composition;

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
    }
}
