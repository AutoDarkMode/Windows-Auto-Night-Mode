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
using AutoDarkModeLib;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Utils.Handlers;

public static class ThemeCollectionHandler
{
    public static readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
    public static readonly string WindowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    //get a list of all files the theme folder contains. If there is no theme-folder, create one.
    //TODO: Do not use the file names, because they are trimmed at 9 characters.
    public static List<ThemeFile> GetUserThemes()
    {
        try
        {
            var files = Directory.EnumerateFiles(ThemeFolderPath, "*.theme", SearchOption.AllDirectories).ToList();
            files = files
                .Where(f => f.EndsWith(".theme") && !f.Contains(Helper.PathUnmanagedDarkTheme) && !f.Contains(Helper.NameUnmanagedLightTheme) && !f.Contains(Helper.PathManagedTheme))
                .ToList();
            var themeFiles = files.Select(f => new ThemeFile(f)).ToList();
            InjectWindowsThemes(themeFiles);
            return themeFiles;
        }
        catch
        {
            Directory.CreateDirectory(ThemeFolderPath);
            return GetUserThemes();
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
