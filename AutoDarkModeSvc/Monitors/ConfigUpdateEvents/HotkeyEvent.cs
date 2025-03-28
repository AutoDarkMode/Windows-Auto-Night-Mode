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
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    class HotkeyEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            if (newConfig.Hotkeys.Enabled && !oldConfig.Hotkeys.Enabled)
            {
                HotkeyHandler.RegisterAllHotkeys(AdmConfigBuilder.Instance());
            }
            else if (!newConfig.Hotkeys.Enabled && oldConfig.Hotkeys.Enabled)
            {
                HotkeyHandler.UnregisterAllHotkeys();
            }
            else if (newConfig.Hotkeys.Enabled)
            {
                bool darkHotkeyChanged = newConfig.Hotkeys.ForceDark != oldConfig.Hotkeys.ForceDark;
                bool lightHotkeyChanged = newConfig.Hotkeys.ForceLight != oldConfig.Hotkeys.ForceLight;
                bool noForceHotkeyChanged = newConfig.Hotkeys.NoForce != oldConfig.Hotkeys.NoForce;
                bool autoThemeSwitchHotkeyChanged = newConfig.Hotkeys.ToggleAutoThemeSwitch != oldConfig.Hotkeys.ToggleAutoThemeSwitch;
                bool toggleThemeHotkeyChanged = newConfig.Hotkeys.ToggleTheme != oldConfig.Hotkeys.ToggleTheme;
                bool togglePostponeHotkeyChanged = newConfig.Hotkeys.TogglePostpone != oldConfig.Hotkeys.TogglePostpone;
                if (darkHotkeyChanged || lightHotkeyChanged || noForceHotkeyChanged || autoThemeSwitchHotkeyChanged || toggleThemeHotkeyChanged || togglePostponeHotkeyChanged)
                {
                    HotkeyHandler.UnregisterAllHotkeys();
                    HotkeyHandler.RegisterAllHotkeys(AdmConfigBuilder.Instance());
                }
            }
        }
    }
}
