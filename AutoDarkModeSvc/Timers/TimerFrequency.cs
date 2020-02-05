using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Timers
{
    static class TimerFrequency
    {
        // short timer is 60s
        public const int Short = 60000;

        // IO Timer is 1h
        public const int IO = 3600000;
        
        //location Timer is 24h
        public const int Location = 86400000;

    }
}
