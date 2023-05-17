#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeLib
{
    public static class Helper
    {
        public const string UpdaterExecutableName = "AutoDarkModeUpdater.exe";
        public const string UpdaterDirName = "adm-updater";
        public const string PostponeItemPauseAutoSwitch = "PauseAutoSwitch";
        public const string PostponeItemDelayAutoSwitch = "DelayAutoSwitch";
        public const string PostponeItemDelayGracePeriod = "SwitchNotification";
        public const string PostponeItemSessionLock = "SessionLock";
        public static readonly string ExecutionPath = GetExecutionPathService();
        public static readonly string ExecutionDir = GetExecutionDir();
        public static readonly string ExecutionPathApp = GetExecutionPathApp();
        public static readonly string ExecutionPathUpdater = GetExecutionPathUpdater();
        public static readonly string ExectuionPathThemeBridge = GetExecutionPathThemeBridge();
        public static readonly string ExectuionPathShell = GetExecutionPathShell();
        public static readonly string ExecutionDirUpdater = GetExecutionDirUpdater();
        public static readonly string UpdateDataDir = GetUpdateDataDir();
        public static string PathThemeFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes");
        public static string PathManagedTheme { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes", "ADMTheme.theme");
        public static string PathDwmRefreshTheme { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes", "DwmRefreshTheme.theme");
        public static string PathUnmanagedDarkTheme { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes", "ADMUnmanagedDark.theme");
        public static string PathUnmanagedLightTheme { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes", "ADMUnmanagedLight.theme");
        public static string NameUnmanagedLightTheme { get; } = "ADMUnmanagedLight";
        public static string NameUnmanagedDarkTheme { get; } = "ADMUnmanagedDark";
        public static string Hegex { get; } = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$";

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
            var assemblyLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var executableName = UpdaterExecutableName;
            var executablePath = Directory.GetParent(assemblyLocation).FullName;
            return Path.Combine(executablePath, UpdaterDirName, executableName);
        }

        private static string GetExecutionPathShell()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeShell.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }


        private static string GetExecutionDirUpdater()
        {
            var assemblyLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var executablePath = Path.Combine(Directory.GetParent(assemblyLocation).FullName, "adm-updater");
            return executablePath;
        }

        private static string GetExecutionPathThemeBridge()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "IThemeManager2Bridge";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetUpdateDataDir()
        {
            var assemblyLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var dataPath = Path.Combine(Directory.GetParent(assemblyLocation).FullName, "adm-update-data");
            return dataPath;
        }

        public static bool InstallModeUsers()
        {
            string pFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            string pFilesx64 = Environment.GetEnvironmentVariable("ProgramFiles");
            return !(ExecutionDir.Contains(pFilesx64) || ExecutionDir.Contains(pFilesx86));
        }

        public static string SerializeLearnedThemesDict(Dictionary<string, string> dict)
        {
            YamlDotNet.Serialization.ISerializer yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            return yamlSerializer.Serialize(dict);
        }

        public static Dictionary<string, string> DeserializeLearnedThemesDict(string data)
        {
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            Dictionary<string, string> deserialized = yamlDeserializer.Deserialize<Dictionary<string, string>>(data);
            return deserialized;
        }
    }

    public static class TimeZoneInfoExtensions
    {
        public static string ToUtcOffsetString(this TimeZoneInfo timeZone)
        {
            var utcOffset = timeZone.BaseUtcOffset;
            var sign = utcOffset < TimeSpan.Zero ? "-" : "+";
            var hours = Math.Abs(utcOffset.Hours).ToString("00");
            var minutes = Math.Abs(utcOffset.Minutes).ToString("00");
            return $"UTC{sign}{hours}:{minutes}";
        }
    }
}
