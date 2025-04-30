#region copyright
// TODO: Should we reduced copyright header? Made it more concise while keeping all important info
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion
using System.Globalization;
using System.Text;
using AutoDarkModeLib;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Utils.Handlers;

public static class ThemeCollectionHandler
{
    public static readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
    public static readonly string WindowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    //  Get a list of all files the theme folder contains. If there is no theme-folder, create one.
    public static List<ThemeFile> GetUserThemes()
    {
        try
        {
            var files = Directory.EnumerateFiles(ThemeFolderPath, "*.theme", SearchOption.AllDirectories).ToList();
            //files = files.Where(f => !f.Contains(Helper.PathUnmanagedDarkTheme) && !f.Contains(Helper.NameUnmanagedLightTheme) && !f.Contains(Helper.PathManagedTheme)).ToList();

            var themeFiles = new List<ThemeFile>();
            foreach (var file in files)
            {
                string displayName = GetThemeDisplayName(file);
                themeFiles.Add(new ThemeFile(file, displayName));
            }

            InjectWindowsThemes(themeFiles);
            return themeFiles;
        }
        catch
        {
            Directory.CreateDirectory(ThemeFolderPath);
            return GetUserThemes();
        }
    }

    // Thanks Jay and Copilot
    private static string GetThemeDisplayName(string themePath)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var lines = File.ReadAllLines(themePath, Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage));
            var displayNameLine = lines.FirstOrDefault(l => l.StartsWith("DisplayName="));
            if (displayNameLine != null)
            {
                return displayNameLine.Split('=')[1].Trim();
            }
            else
            {
                return Path.GetFileNameWithoutExtension(themePath) ?? "Undefined";
            }
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(themePath) ?? "Undefined";
        }
    }

    private static void InjectWindowsThemes(List<ThemeFile> themeFiles)
    {
        if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
        {
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\aero.theme"), AdmProperties.Resources.ThemePickerTheme11Light));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\dark.theme"), AdmProperties.Resources.ThemePickerTheme11Dark));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\themeA.theme"), AdmProperties.Resources.ThemePickerTheme11Glow));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\themeB.theme"), AdmProperties.Resources.ThemePickerTheme11CapturedMotion));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\themeC.theme"), AdmProperties.Resources.ThemePickerTheme11Sunrise));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\themeD.theme"), AdmProperties.Resources.ThemePickerTheme11Flow));
            ThemeFile spotlight = new(Path.Combine(WindowsPath, @"Resources\Themes\spotlight.theme"), AdmProperties.Resources.ThemePickerTheme11Spotlight);
            if (File.Exists(spotlight.Path))
            {
                themeFiles.Add(spotlight);
            }
        }
        else
        {
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\aero.theme"), AdmProperties.Resources.ThemePickerTheme10Windows));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\Light.theme"), AdmProperties.Resources.ThemePickerTheme10WindowsLight));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\theme1.theme"), AdmProperties.Resources.ThemePickerTheme10Windows10));
            themeFiles.Add(new ThemeFile(Path.Combine(WindowsPath, @"Resources\Themes\theme2.theme"), AdmProperties.Resources.ThemePickerTheme10Flowers));
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

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is string name ? Name.Equals(name, StringComparison.Ordinal) : base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
