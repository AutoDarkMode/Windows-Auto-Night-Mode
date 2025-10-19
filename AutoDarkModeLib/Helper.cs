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
using Windows.System;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoDarkModeLib;

public static class Helper
{
    public const string MissingWallpaperFileName = "AutoDarkModeMissingWallpaper.png";
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
    public static readonly string ExecutionPathThemeBridge = GetExecutionPathThemeBridge();
    public static readonly string ExecutionPathShell = GetExecutionPathShell();
    public static readonly string ExecutionDirUpdater = GetExecutionDirUpdater();
    public static readonly string ExecutionPathService = GetExecutionPathService();
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

    private static string GetValidatedBasePath()
    {
        var currentPath = AppContext.BaseDirectory;
        var directoryInfo = new DirectoryInfo(currentPath);

        // Check if current directory is "core"
        if (directoryInfo.Name.Equals("ui", StringComparison.OrdinalIgnoreCase) ||
            directoryInfo.Name.Equals("core", StringComparison.OrdinalIgnoreCase))
        {
            directoryInfo = directoryInfo.Parent ?? throw new InvalidOperationException("Parent directory is missing.");
        }

        if (!directoryInfo.Name.Equals("adm-app", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected directory 'adm-app' but found '{directoryInfo.Name}'.");
        }

        return directoryInfo.FullName;
    }

    private static string GetExecutionPathService()
    {
        var assemblyLocation = GetValidatedBasePath();
        return Path.Combine(assemblyLocation, "core", "AutoDarkModeSvc.exe");
    }

    private static string GetExecutionPathApp()
    {
        var assemblyLocation = GetValidatedBasePath();
        return Path.Combine(assemblyLocation, "ui", "AutoDarkModeApp.exe");
    }

    private static string GetExecutionPathUpdater()
    {
        var assemblyLocation = GetValidatedBasePath().TrimEnd(Path.DirectorySeparatorChar);
        var executableName = UpdaterExecutableName;
        var executablePath = Directory.GetParent(assemblyLocation).FullName;
        return Path.Combine(executablePath, UpdaterDirName, executableName);
    }

    private static string GetExecutionPathShell()
    {
        var assemblyLocation = GetValidatedBasePath();
        return Path.Combine(assemblyLocation, "core", "AutoDarkModeShell.exe");
    }

    private static string GetExecutionDir()
    {
        var assemblyLocation = GetValidatedBasePath();
        var executablePath = Path.GetDirectoryName(assemblyLocation);
        return executablePath;
    }


    private static string GetExecutionDirUpdater()
    {
        var assemblyLocation = GetValidatedBasePath().TrimEnd(Path.DirectorySeparatorChar);
        var executablePath = Path.Combine(Directory.GetParent(assemblyLocation).FullName, "adm-updater");
        return executablePath;
    }

    private static string GetExecutionPathThemeBridge()
    {
        var assemblyLocation = GetValidatedBasePath();
        return Path.Combine(assemblyLocation, "core", "IThemeManager2Bridge");
    }

    private static string GetUpdateDataDir()
    {
        var assemblyLocation = GetValidatedBasePath().TrimEnd(Path.DirectorySeparatorChar);
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

    public static string GetMissingWallpaperPath()
    {
        var assemblyLocation = GetValidatedBasePath();
        return Path.Combine(assemblyLocation, "core", "Assets", MissingWallpaperFileName);
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

public static class HotkeyStringConverter
{
    private static readonly Dictionary<string, uint> ModifierMap = new()
    {
        { "Alt", 1 },
        { "Ctrl", 2 },
        { "Shift", 4 },
        { "Win", 8 }
    };

    private static readonly Dictionary<string, string> WinFormsKeyMap = new()
    {
        { "Control", "Ctrl" },
        { "LWin", "Win" },
        { "RWin", "Win" },
        { "Back", "Backspace" },
        { "Return", "Enter" },
        { "Prior", "PgUp" },
        { "Next", "PgDn" },
        { "Capital", "CapsLock" },
        { "Oemcomma", "," },
        { "OemPeriod", "." },
        { "OemQuestion", "/" },
        { "Oemplus", "=" },
        { "OemMinus", "-" },
        { "OemOpenBrackets", "[" },
        { "OemCloseBrackets", "]" },
        { "OemPipe", "\\" },
        { "OemSemicolon", ";" },
        { "OemQuotes", "'" },
        { "Oemtilde", "`" }
    };

    private static readonly Dictionary<string, VirtualKey> SpecialKeyMap = new()
    {
        { "Enter", VirtualKey.Enter },
        { "Esc", VirtualKey.Escape },
        { "Space", VirtualKey.Space },
        { "Backspace", VirtualKey.Back },
        { "Del", VirtualKey.Delete },
        { "PgUp", VirtualKey.PageUp },
        { "PgDn", VirtualKey.PageDown },
        { "CapsLock", VirtualKey.CapitalLock },
        { "Tab", VirtualKey.Tab },
        { "Home", VirtualKey.Home },
        { "End", VirtualKey.End },
        { "Insert", VirtualKey.Insert },
        { "Up", VirtualKey.Up },
        { "Down", VirtualKey.Down },
        { "Left", VirtualKey.Left },
        { "Right", VirtualKey.Right },
        { ",", (VirtualKey)188 },
        { ".", (VirtualKey)190 },
        { "/", (VirtualKey)191 },
        { "=", (VirtualKey)187 },
        { "-", (VirtualKey)189 },
        { "[", (VirtualKey)219 },
        { "]", (VirtualKey)221 },
        { "\\", (VirtualKey)220 },
        { ";", (VirtualKey)186 },
        { "'", (VirtualKey)222 },
        { "`", (VirtualKey)192 }
    };

    private static readonly Dictionary<VirtualKey, string> ReverseSpecialKeyMap = SpecialKeyMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static bool IsWinFormsFormat(string? hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return false;
        }

        return (hotkeyString.Contains("Control") ||
                hotkeyString.Contains("LWin") ||
                hotkeyString.Contains("RWin") ||
                hotkeyString.Contains("Return") ||
                hotkeyString.Contains("Oem")) &&
               !hotkeyString.Contains(" + ");
    }

    private static string NormalizeKeyName(string keyName)
    {
        if (WinFormsKeyMap.TryGetValue(keyName, out string normalized))
        {
            return normalized;
        }
        return keyName;
    }

    public static string ToDisplayFormat(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return null;
        }

        try
        {
            var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            List<string> displayParts = [];
            string mainKey = null;

            foreach (var part in parts)
            {
                var normalizedPart = NormalizeKeyName(part);

                if (ModifierMap.ContainsKey(normalizedPart))
                {
                    displayParts.Add(normalizedPart);
                }
                else
                {
                    mainKey = normalizedPart;
                }
            }

            if (string.IsNullOrEmpty(mainKey))
            {
                return null;
            }

            var orderedModifiers = displayParts.Distinct().OrderBy(m => m switch
            {
                "Ctrl" => 0,
                "Shift" => 1,
                "Alt" => 2,
                "Win" => 3,
                _ => 4
            });

            var result = string.Join(" + ", orderedModifiers.Append(mainKey));
            return result;
        }
        catch
        {
            return hotkeyString;
        }
    }

    public static (uint modifiers, uint keyCode)? Parse(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return null;
        }

        try
        {
            var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            uint modifiers = 0;
            string mainKey = null;

            foreach (var part in parts)
            {
                var normalizedPart = NormalizeKeyName(part);

                if (ModifierMap.TryGetValue(normalizedPart, out uint modifierValue))
                {
                    modifiers |= modifierValue;
                }
                else
                {
                    mainKey = normalizedPart;
                }
            }

            if (string.IsNullOrEmpty(mainKey))
            {
                return null;
            }

            uint keyCode = GetKeyCode(mainKey);
            if (keyCode == 0)
            {
                return null;
            }

            return (modifiers, keyCode);
        }
        catch
        {
            return null;
        }
    }

    private static uint GetKeyCode(string keyName)
    {
        if (SpecialKeyMap.TryGetValue(keyName, out VirtualKey specialKey))
        {
            return (uint)specialKey;
        }

        if (keyName.StartsWith('F') && int.TryParse(keyName[1..], out int fNum) && fNum >= 1 && fNum <= 12)
        {
            return (uint)(VirtualKey.F1 + fNum - 1);
        }

        if (keyName.StartsWith("NumPad") && int.TryParse(keyName[6..], out int numPadNum) && numPadNum >= 0 && numPadNum <= 9)
        {
            return (uint)(VirtualKey.NumberPad0 + numPadNum);
        }

        if (keyName.StartsWith("Number") && int.TryParse(keyName[6..], out int numNum) && numNum >= 0 && numNum <= 9)
        {
            return (uint)(VirtualKey.Number0 + numNum);
        }

        if (keyName.Length == 1 && char.IsLetter(keyName[0]))
        {
            char upper = char.ToUpper(keyName[0]);
            return (uint)upper;
        }

        if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
        {
            return (uint)(VirtualKey.Number0 + (keyName[1] - '0'));
        }

        return 0;
    }

    public static string GetKeyDisplayName(VirtualKey key)
    {
        if (ReverseSpecialKeyMap.TryGetValue(key, out string? specialName))
        {
            return specialName;
        }

        if (key >= VirtualKey.F1 && key <= VirtualKey.F12)
        {
            return $"F{(int)key - (int)VirtualKey.F1 + 1}";
        }

        if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
        {
            return $"NumPad{(int)key - (int)VirtualKey.NumberPad0}";
        }

        // Numbers 0-9 ToString() is Number+num

        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return key.ToString();
        }

        return key.ToString();
    }
}
