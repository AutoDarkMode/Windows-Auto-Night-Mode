#region copyright
// TODO: Should we reduced copyright header? Made it more concise while keeping all important info
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion
using System.Runtime.InteropServices;
using System.Text;
using AutoDarkModeApp.Helpers;
using AutoDarkModeLib;

namespace AutoDarkModeApp.Utils.Handlers;

public static class ThemeCollectionHandler
{
    public static readonly string UserThemesFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Themes");
    public static readonly string WindowsThemesFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Resources", "Themes");

    // friendly names for built-in Windows themes (because Windows does NOT store these anywhere)
    private static readonly Dictionary<string, string> WindowsThemeNameOverrides = new()
    {
        // Windows 11
        { "aero", "Theme11_Light".GetLocalized() },
        { "dark", "Theme11_Dark".GetLocalized() },
        { "themeA", "Theme11_Glow".GetLocalized() },
        { "themeB", "Theme11_CapturedMotion".GetLocalized() },
        { "themeC", "Theme11_Sunrise".GetLocalized() },
        { "themeD", "Theme11_Flow".GetLocalized() },
        { "spotlight", "Theme11_Spotlight".GetLocalized() },

        // Windows 10
        { "aero_Win10", "Theme10_Windows".GetLocalized() },          // same filename as Win11, different meaning
        { "light", "Theme10_WindowsLight".GetLocalized() },
        { "theme1", "Theme10_Windows10".GetLocalized() },
        { "theme2", "Theme10_Flowers".GetLocalized() }
    };

    //  Get a list of all files the theme folder contains. If there is no theme-folder, create one.
    public static List<ThemeFile> GetUserThemes()
    {
        try
        {
            var files = Directory.EnumerateFiles(UserThemesFolderPath, "*.theme", SearchOption.AllDirectories).ToList()
            .Where(f => !f.Contains(Helper.PathUnmanagedDarkTheme) && !f.Contains(Helper.NameUnmanagedLightTheme) && !f.Contains(Helper.PathManagedTheme)).ToList();

            var themeFiles = new List<ThemeFile>();

            // ---------------------------------------------------------
            // User themes
            // ---------------------------------------------------------

            foreach (var file in files)
            {
                string displayName = GetThemeDisplayName(file);
                themeFiles.Add(new ThemeFile(file, displayName));
            }

            // ---------------------------------------------------------
            // Built‑in Windows themes (Win10 + Win11)
            // ---------------------------------------------------------
            if (Directory.Exists(WindowsThemesFolderPath))
            {

                foreach (var file in Directory.EnumerateFiles(WindowsThemesFolderPath, "*.theme", SearchOption.TopDirectoryOnly))
                {
                    string displayName = GetThemeDisplayName(file).Trim();
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    bool isWin11 = Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC;
                    string lookupKey = fileName;

                    // skip ADM-managed themes
                    if (file.Contains(Helper.PathUnmanagedDarkTheme) ||
                        file.Contains(Helper.PathUnmanagedLightTheme) ||
                        file.Contains(Helper.PathManagedTheme)) continue;

                    // Microsoft doesn’t store friendly names inside the .theme files
                    // "DisplayName" inside the .theme file is not a friendly name
                    // If Windows stored a resource reference instead of a real name, ignore it
                    if (displayName.StartsWith("@%SystemRoot%", StringComparison.OrdinalIgnoreCase))
                        displayName = "";

                    if (!isWin11 && fileName == "aero")
                    {
                        lookupKey = "aero_Win10"; // rename for Windows 10
                    }

                    if (string.IsNullOrEmpty(displayName) && WindowsThemeNameOverrides.TryGetValue(lookupKey, out string friendly))
                        displayName = friendly;

                    // fallback to filename if display name is empty
                    if (string.IsNullOrEmpty(displayName))
                        displayName = fileName;

                    themeFiles.Add(new ThemeFile(file, displayName));
                }
            }
            return themeFiles;
        }
        catch
        {
            Directory.CreateDirectory(UserThemesFolderPath);
            return GetUserThemes();
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPrivateProfileString(
        string section,
        string key,
        string defaultValue,
        StringBuilder retVal,
        int size,
        string filePath);

    private static string GetThemeDisplayName(string themePath)
    {
        try
        {
            StringBuilder displayName = new StringBuilder(255);
            _ = GetPrivateProfileString("Theme", "DisplayName", "", displayName, displayName.Capacity, themePath);

            string name = displayName.ToString();
            if (!string.IsNullOrEmpty(name))
                return name;

            // fallback: filename
            return Path.GetFileNameWithoutExtension(themePath) ?? "Undefined";
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(themePath) ?? "Undefined";
        }
    }
}

public class ThemeFile(string path)
{
    public ThemeFile(string path, string name)
        : this(path)
    {
        Name = name;
        IsWindowsTheme = true;
    }

    public string Path { get; } = path;
    public string Name { get; } = System.IO.Path.GetFileNameWithoutExtension(path) ?? "Undefined";
    public bool IsWindowsTheme { get; }

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        return obj is string name ? Name.Equals(name, StringComparison.Ordinal) : base.Equals(obj);
    }

    public override int GetHashCode() => Name.GetHashCode();
}
