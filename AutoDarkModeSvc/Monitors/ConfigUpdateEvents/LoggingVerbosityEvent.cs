using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    internal class LoggingVerbosityEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            bool debugToggled = newConfig.Tunable.Debug != oldConfig.Tunable.Debug;
            bool traceToggled = newConfig.Tunable.Trace != oldConfig.Tunable.Trace;
            if (debugToggled || traceToggled) LoggerSetup.UpdateLogmanConfig();
        }
    }
}
