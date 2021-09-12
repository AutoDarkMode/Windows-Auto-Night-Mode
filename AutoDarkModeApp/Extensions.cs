using System;
using System.IO;
using System.Threading.Tasks;

namespace AutoDarkModeSvc
{
    public enum Mode
    {
        Switch = 0,
        LightOnly = 1,
        DarkOnly = 2,
        Bluelight = 3
    };
    public enum Theme
    {
        Ignore = -2,
        Undefined = -1,
        Dark = 0,
        Light = 1
    };

    public static class Extensions
    {
        public static readonly string ExecutionPath = GetExecutionPath();
        public static readonly string ExecutionDir = GetExecutionDir();

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

        public static bool TimeisBetweenTimes(TimeSpan time, TimeSpan start, TimeSpan end)
        {
            if (start == end)
            {
                return true;
            }

            if (start <= end)
            {
                // start and stop times are in the same day
                if (time >= start && time <= end)
                {
                    // current time is between start and stop
                    return true;
                }
            }
            else
            {
                // start and stop times are in different days
                if (time >= start || time <= end)
                {
                    // current time is between start and stop
                    return true;
                }
            }

            return false;
        }

        private static string GetExecutionPath()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableName = Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(assemblyLocation) + ".exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }

    }
}
