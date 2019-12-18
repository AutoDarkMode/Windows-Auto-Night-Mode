using System;
using System.Threading.Tasks;

namespace AutoDarkModeSvc
{
    public enum Mode
    {
        Switch = 0,
        LightOnly = 1,
        DarkOnly = 2
    };
    public enum Theme
    {
        Undefined = -1,
        Dark = 0,
        Light = 1
    };

    public static class Extensions
    {
 
        public static bool NowIsBetweenTimes(TimeSpan start, TimeSpan end)
        {
            if (start == end)
            {
                return true;
            }

            TimeSpan now = DateTime.Now.TimeOfDay;

            if (start <= end)
            {
                // start and stop times are in the same day
                if (now >= start && now <= end)
                {
                    // current time is between start and stop
                    return true;
                }
            }
            else
            {
                // start and stop times are in different days
                if (now >= start || now <= end)
                {
                    // current time is between start and stop
                    return true;
                }
            }

            return false;
        }
    }
}
