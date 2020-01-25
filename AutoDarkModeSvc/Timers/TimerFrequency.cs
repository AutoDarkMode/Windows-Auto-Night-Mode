using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Timers
{
    static class TimerFrequency
    {
        // short timer is 10s
        public const int Short = 60000;
        // IO Timer is 1h

        //public static readonly int IO = 3600000;
        public const int IO = 30000;
        
        //location Timer is 24h
        public const int Location = 86400000;

    }
}
