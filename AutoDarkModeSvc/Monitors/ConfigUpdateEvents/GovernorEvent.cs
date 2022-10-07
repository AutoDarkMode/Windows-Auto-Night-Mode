using AutoDarkModeLib;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    internal class GovernorEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            if (oldConfig.Governor != newConfig.Governor)
            {
                if (State.PostponeManager.GetSkipNextSwitchItem() != null)
                {
                    State.PostponeManager.RemoveSkipNextSwitch();
                    State.PostponeManager.AddSkipNextSwitch();
                }
            }
        }
    }
}
