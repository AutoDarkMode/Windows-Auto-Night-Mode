using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    internal class AutoSwitchToggledEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            if (!oldConfig.AutoThemeSwitchingEnabled && newConfig.AutoThemeSwitchingEnabled)
            {
                // run all enable hooks to allow changes to be propagated to the managed mode if they were changed
                // externally while auto theme switching was disabled.
                // This applies when ADM was kept open while those changes were made.
                ComponentManager.Instance().RunAllEnableHooks();
            }
        }

    }
}
