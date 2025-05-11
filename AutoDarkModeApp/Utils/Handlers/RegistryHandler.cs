#region copyright
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion

using Microsoft.Win32;

namespace AutoDarkModeApp.Utils.Handlers;

internal static class RegistryHandler
{
    private const string WindowsNtCurrentVersionPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string DwmPath = @"Software\Microsoft\Windows\DWM";

    // Cache registry values to avoid repeated access
    private static readonly Lazy<string> _osVersion = new(() => GetRegistryValue(WindowsNtCurrentVersionPath, "ReleaseId", "") ?? "");

    private static readonly Lazy<string> _ubr = new(() => GetRegistryValue(WindowsNtCurrentVersionPath, "UBR", "0") ?? "0");

    // Get windows version number, like 1607 or 1903
    public static string GetOSversion() => _osVersion.Value;

    public static string GetUbr() => _ubr.Value;

    public static bool IsDWMPrevalence()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(DwmPath);
            if (key == null)
                return true;

            object? value = key.GetValue("ColorPrevalence");
            return value is int i && i == 1;
        }
        catch
        {
            return true;
        }
    }

    private static string GetRegistryValue(string path, string name, string defaultValue)
    {
        try
        {
            return Registry.GetValue(path, name, defaultValue)?.ToString() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
