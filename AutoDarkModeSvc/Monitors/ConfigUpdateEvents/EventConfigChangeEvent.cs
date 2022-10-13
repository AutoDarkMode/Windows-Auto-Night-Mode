using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    public class EventConfigChangeEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            if (oldConfig.Events.Win10AllowLockscreenSwitch != newConfig.Events.Win10AllowLockscreenSwitch)
            {
                SystemEventHandler.DeregisterResumeEvent();
                SystemEventHandler.RegisterResumeEvent();
            }
        }
    }

}
