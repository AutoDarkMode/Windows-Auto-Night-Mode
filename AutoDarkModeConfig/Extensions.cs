using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AutoDarkModeConfig
{
    public enum Mode
    {   
        Switch = 0,
        LightOnly = 1,
        DarkOnly = 2,
        AccentOnly = 3
    };
    public enum Theme
    {
        Ignore = -2,
        Unknown = -1,
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

    public enum SwitchSource
    {
        Any,
        TimeSwitchModule,
        BatteryStatusChanged,
        SystemResume,
        Manual,
        ExternalThemeSwitch
    }

    public static class Extensions
    {
        public const string UpdaterExecutableName = "AutoDarkModeUpdater.exe";
        public const string UpdaterDirName = "Updater";
        public static readonly string ExecutionPath = GetExecutionPathService();
        public static readonly string ExecutionDir = GetExecutionDir();
        public static readonly string ExecutionPathApp = GetExecutionPathApp();
        public static readonly string ExecutionPathUpdater = GetExecutionPathUpdater();
        public static readonly string ExecutionDirUpdater = GetExecutionDirUpdater();
        public static readonly string UpdateDataDir = GetUpdateDataDir();

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

        public static string CommitHash()
        {
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string productVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
                string commitHash = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion[(productVersion.LastIndexOf("-") + 2)..];
                return commitHash;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// checks whether a time is within a grace period (within x minutes around a DateTime)
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

        private static string GetExecutionPathService()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionPathApp()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeApp.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionPathUpdater()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = UpdaterExecutableName;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath, UpdaterDirName, executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }


        private static string GetExecutionDirUpdater()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath, UpdaterDirName);
        }

        private static string GetUpdateDataDir()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var dataPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "UpdateData");
            return dataPath;
        }

        public static bool InstallModeUsers()
        {
            string pFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            string pFilesx64 = Environment.GetEnvironmentVariable("ProgramFiles");
            return !(ExecutionDir.Contains(pFilesx64) || ExecutionDir.Contains(pFilesx86));
        }

    }
}
