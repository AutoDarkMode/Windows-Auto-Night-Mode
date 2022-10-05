using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                bool darkHotkeyChanged = newConfig.Hotkeys.ForceDarkHotkey != oldConfig.Hotkeys.ForceDarkHotkey;
                bool lightHotkeyChanged = newConfig.Hotkeys.ForceLightHotkey != oldConfig.Hotkeys.ForceLightHotkey;
                bool noForceHotkeyChanged = newConfig.Hotkeys.NoForceHotkey != oldConfig.Hotkeys.NoForceHotkey;
                bool autoThemeSwitchHotkeyChanged = newConfig.Hotkeys.ToggleAutoThemeSwitchingHotkey != oldConfig.Hotkeys.ToggleAutoThemeSwitchingHotkey;
                if (darkHotkeyChanged || lightHotkeyChanged || noForceHotkeyChanged || autoThemeSwitchHotkeyChanged)
                {
                    HotkeyHandler.UnregisterAllHotkeys();
                    HotkeyHandler.RegisterAllHotkeys(AdmConfigBuilder.Instance());
                }
            }
        }
    }
}
