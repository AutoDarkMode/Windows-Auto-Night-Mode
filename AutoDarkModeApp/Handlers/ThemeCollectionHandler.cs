using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Handlers
{
    public static class ThemeCollectionHandler
    {
        public static readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";

        //get a list of all files the theme folder contains. If there is no theme-folder, create one.
        public static List<ThemeFile> GetUserThemes()
        {
            try
            {
                List<string> files = Directory.EnumerateFiles(ThemeFolderPath, "*.*", SearchOption.AllDirectories).ToList();
                files = files.Where(f => f.EndsWith(".theme")).ToList();
                List<ThemeFile> themeFiles = files.Select(f => new ThemeFile(f)).ToList();
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
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\aero.theme", Properties.Resources.ThemePickerTheme11Light));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\dark.theme", Properties.Resources.ThemePickerTheme11Dark));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\themeA.theme", Properties.Resources.ThemePickerTheme11Glow));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\themeB.theme", Properties.Resources.ThemePickerTheme11CapturedMotion));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\themeC.theme", Properties.Resources.ThemePickerTheme11Sunrise));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\themeD.theme", Properties.Resources.ThemePickerTheme11Flow));

            }
            else
            {
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\aero.theme", Properties.Resources.ThemePickerTheme10Windows));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\Light.theme", Properties.Resources.ThemePickerTheme10WindowsLight));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\theme1.theme", Properties.Resources.ThemePickerTheme10Windows10));
                themeFiles.Add(new ThemeFile(@"C:\Windows\Resources\Themes\theme2.theme", Properties.Resources.ThemePickerTheme10Flowers));
            }
        }
    }

    public class ThemeFile
    {
        public ThemeFile(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path) ?? "Undefined";
        }
        public ThemeFile(string path, string name) : this(path)
        {
            Name = name;
        }

        public string Path { get; }
        public string Name { get; }
        public override string ToString()
        {
            return Name;
        }
        public override bool Equals(object obj)
        {
            return obj is string name ? Name.Equals(name, StringComparison.Ordinal) : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
