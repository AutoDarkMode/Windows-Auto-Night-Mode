using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Timers
{
    static class TimerFrequency
    {
        // short timer is 60s
        public const int Short = 60000;
        // IO Timer is 2h
        public const int IO = 7200000;
        //location Timer is 1h
        public const int Location = 3600000;
        //update timer for system state
        public const int StateUpdate = 300000;

    }
}
