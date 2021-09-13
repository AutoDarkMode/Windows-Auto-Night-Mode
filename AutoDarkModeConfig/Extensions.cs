using System;
using System.IO;
using System.Threading.Tasks;

namespace AutoDarkModeConfig
{
    public enum Mode
    {   
        Switch = 0,
        LightOnly = 1,
        DarkOnly = 2
    };
    public enum Theme
    {
        Ignore = -2,
        Undefined = -1,
        Dark = 0,
        Light = 1
    };

    /// <summary>
    /// This enumeration indicates the wallpaper position for all monitors. (This includes when slideshows are running.)
    /// The wallpaper position specifies how the image that is assigned to a monitor should be displayed.
    /// </summary>
    public enum WallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    public static class Extensions
    {
        public static readonly string ExecutionPath = GetExecutionPath();
        public static readonly string ExecutionDir = GetExecutionDir();
        public static readonly string ExecutionPathApp = GetExecutionPathApp();

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

        /// <summary>
        /// checks whether a time is within a grace period (within x minutes before a DateTime)
        /// </summary>
        /// <param name="time">time to be checked</param>
        /// <param name="grace">the grace period</param>
        /// <returns>true if it's within the span; false otherwise</returns>
        public static bool SuntimeIsWithinSpan(DateTime time, int grace)
        {
            return NowIsBetweenTimes(
                time.AddMinutes(-Math.Abs(grace)).TimeOfDay,
                time.AddMinutes(Math.Abs(grace)).TimeOfDay);
        }

        private static string GetExecutionPath()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionPathApp()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeApp.exe";
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
