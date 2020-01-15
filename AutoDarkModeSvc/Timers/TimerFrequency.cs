using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Timers
{
    static class TimerFrequency
    {
        // short timer is 10s
        public static readonly int Short = 10000;
        // IO Timer is 1h

        //public static readonly int IO = 3600000;
        public static readonly int IO = 30000;
        
        //location Timer is 24h
        public static readonly int Location = 86400000;

    }
}
