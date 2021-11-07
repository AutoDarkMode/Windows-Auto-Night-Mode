using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config.ConfigUpdateEvents
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
                if (darkHotkeyChanged || lightHotkeyChanged || noForceHotkeyChanged)
                {
                    HotkeyHandler.UnregisterAllHotkeys();
                    HotkeyHandler.RegisterAllHotkeys(AdmConfigBuilder.Instance());
                }
            }
        }
    }
}
