using AutoDarkModeLib;
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
                bool darkHotkeyChanged = newConfig.Hotkeys.ForceDark != oldConfig.Hotkeys.ForceDark;
                bool lightHotkeyChanged = newConfig.Hotkeys.ForceLight != oldConfig.Hotkeys.ForceLight;
                bool noForceHotkeyChanged = newConfig.Hotkeys.NoForce != oldConfig.Hotkeys.NoForce;
                bool autoThemeSwitchHotkeyChanged = newConfig.Hotkeys.ToggleAutoThemeSwitch != oldConfig.Hotkeys.ToggleAutoThemeSwitch;
                bool toggleThemeHotkeyChanged = newConfig.Hotkeys.ToggleTheme != oldConfig.Hotkeys.ToggleTheme;
                if (darkHotkeyChanged || lightHotkeyChanged || noForceHotkeyChanged || autoThemeSwitchHotkeyChanged || toggleThemeHotkeyChanged)
                {
                    HotkeyHandler.UnregisterAllHotkeys();
                    HotkeyHandler.RegisterAllHotkeys(AdmConfigBuilder.Instance());
                }
            }
        }
    }
}
