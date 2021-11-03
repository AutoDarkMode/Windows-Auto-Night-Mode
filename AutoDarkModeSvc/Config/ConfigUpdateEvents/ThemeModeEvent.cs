using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config.ConfigUpdateEvents
{
    class ThemeModeEvent : ConfigUpdateEvent<AdmConfig>
    {
        private readonly ComponentManager cm;
        public ThemeModeEvent(ComponentManager cm) {
            this.cm = cm;
        }
        protected override void ChangeEvent()
        {
            bool themeModeToggled = newConfig.WindowsThemeMode.Enabled != oldConfig.WindowsThemeMode.Enabled;
            // if the theme mode is toggled to off, we need to reinitialize all components
            if (themeModeToggled && newConfig.WindowsThemeMode.Enabled)
            {
                cm.InvokeDisableHooks();
            }
        }
    }
}
