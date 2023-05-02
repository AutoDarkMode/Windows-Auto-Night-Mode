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
using AutoDarkModeSvc.Handlers.IThemeManager2;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Devices.PointOfService;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    public partial class ThemeFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool themeFixShouldAdd = false;
        public ThemeFile(string path)
        {
            Random rnd = new Random();
            int val = rnd.Next(0, 2);
            if (val == 0) themeFixShouldAdd = true;
            ThemeFilePath = path;
        }
        public Encoding Encoding { get; set; } = Encoding.GetEncoding(1252);
        public Colors Colors { get; set; } = new();
        public Cursors Cursors { get; set; } = new();
        public Desktop Desktop { get; set; } = new();
        public string DisplayName { get; set; } = "ADMTheme";
        public MasterThemeSelector MasterThemeSelector { get; set; } = new();
        public Slideshow Slideshow { get; set; } = new();
        public List<string> ThemeFileContent { get; private set; } = new();
        public string ThemeFilePath { get; private set; }
        public string ThemeId { get; set; } = $"{{{Guid.NewGuid()}}}";
        public string UnmanagedOriginalName { get; set; } = "undefined";
        public VisualStyles VisualStyles { get; set; } = new();

        public static (List<string>, Encoding, string) GetDisplayNameFromRaw(string themePath)
        {
            List<string> lines = new();
            string pathThemeName = null;
            Encoding encoding;
            (encoding, lines) = Read(themePath);
            pathThemeName = lines.Where(x => x.StartsWith($"{nameof(DisplayName)}".Trim())).FirstOrDefault();
            if (pathThemeName != null) pathThemeName = pathThemeName.Split("=")[1];
            return (lines, encoding, pathThemeName.Trim());
        }

        public static string GetOriginalNameFromRaw(string themePath)
        {
            List<string> lines = new();
            string originalName = null;
            Encoding encoding;
            (encoding, lines) = Read(themePath);
            originalName = lines.Where(x => x.StartsWith($"{nameof(UnmanagedOriginalName)}".Trim())).FirstOrDefault();
            if (originalName != null) originalName = originalName.Split("=")[1];
            else originalName = "";
            return originalName.Trim();
        }

        public static ThemeFile LoadUnmanagedTheme(string sourcePath, string targetPath)
        {
            ThemeFile source = new(sourcePath);
            source.Load();

            ThemeFile target = new(targetPath);
            target.SetContentAndParse(source.ThemeFileContent);
            target.RefreshGuid();
            return target;
        }

        public static void PatchColorsWin11AndSave(ThemeFile theme, string data = null)
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_22H2)
            {
                if (data != null) theme.Colors.InfoText = (data, theme.Colors.InfoText.Item2);
                PatchColorsWin11InMemory(theme);
            }
            theme.Save(managed: false);
        }

        public static void PatchColorsWin11InMemory(ThemeFile theme)
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_22H2)
            {
                string[] rgb = theme.Colors.InfoText.Item1.Split(" ");
                _ = int.TryParse(rgb[0], out int r);
                _ = int.TryParse(rgb[1], out int g);
                _ = int.TryParse(rgb[2], out int b);

                if (theme.themeFixShouldAdd)
                {
                    if (r == 255)
                    {
                        r--;
                    }
                    else
                    {
                        r++;
                        theme.themeFixShouldAdd = false;
                    }
                }
                else if (!theme.themeFixShouldAdd)
                {
                    if (r == 0)
                    {
                        r++;
                    }
                    else
                    {
                        r--;
                        theme.themeFixShouldAdd = true;
                    }
                }
                Logger.Debug($"patched colors from [{theme.Colors.InfoText.Item1}] to [{r} {g} {b}]");
                theme.Colors.InfoText = ($"{r} {g} {b}", theme.Colors.InfoText.Item2);
            }
        }

        /// <summary>
        /// Determines whether Auto Colorization is enabled
        /// </summary>
        /// <returns>true if enabled; false otherwise</returns>
        public bool GetAutoColorizationState()
        {
            return VisualStyles.AutoColorization.Item1 == "1";
        }

        public void Load()
        {
            (Encoding, ThemeFileContent) = Read(ThemeFilePath);
            Parse();
        }

        public void RefreshGuid()
        {
            ThemeId = $"{{{Guid.NewGuid()}}}";
        }

        /// <summary>
        /// Writes the theme file to disk at the specified ThemeFilePath
        /// </summary>
        /// <param name="managed"></param>
        public void Save(bool managed = true)
        {
            UpdateValue("[Theme]", nameof(ThemeId), ThemeId);
            UpdateValue("[Theme]", nameof(DisplayName), DisplayName);
            UpdateValue(Colors.Section.Item1, nameof(Colors.InfoText), Colors.InfoText.Item1);

            if (!managed)
            {
                UpdateValue("[Theme]", nameof(UnmanagedOriginalName), UnmanagedOriginalName);
            }

            if (managed)
            {
                UpdateSection(Cursors.Section.Item1, GetClassFieldsAndValues(Cursors));
                UpdateSection(VisualStyles.Section.Item1, GetClassFieldsAndValues(VisualStyles));
                UpdateValue(Colors.Section.Item1, nameof(Colors.Background), Colors.Background.Item1);
                UpdateValue(MasterThemeSelector.Section.Item1, nameof(MasterThemeSelector.MTSM), MasterThemeSelector.MTSM);

                //Update Desktop class manually due to the way it is internally represented
                List<string> desktopSerialized = new()
                {
                    Desktop.Section.Item1,
                    $"{nameof(Desktop.Wallpaper)}={Desktop.Wallpaper}",
                    $"{nameof(Desktop.Pattern)}={Desktop.Pattern}",
                    $"{nameof(Desktop.MultimonBackgrounds)}={Desktop.MultimonBackgrounds}",
                    $"{nameof(Desktop.WindowsSpotlight)}={Desktop.WindowsSpotlight}",
                    $"{nameof(Desktop.PicturePosition)}={Desktop.PicturePosition}"
                };
                Desktop.MultimonWallpapers.ForEach(w => desktopSerialized.Add($"Wallpaper{w.Item2}={w.Item1}"));
                UpdateSection(Desktop.Section.Item1, desktopSerialized);

                //Update Slideshow
                if (Slideshow.Enabled)
                {
                    List<string> slideshowSerialized = new()
                    {
                        Slideshow.Section.Item1,
                        $"{nameof(Slideshow.Interval)}={Slideshow.Interval}",
                        $"{nameof(Slideshow.Shuffle)}={Slideshow.Shuffle}"
                    };
                    if (Slideshow.ImagesRootPath != null) slideshowSerialized.Add($"{nameof(Slideshow.ImagesRootPath)}={Slideshow.ImagesRootPath}");
                    if (Slideshow.ImagesRootPIDL != null) slideshowSerialized.Add($"{nameof(Slideshow.ImagesRootPIDL)}={Slideshow.ImagesRootPIDL}");
                    Slideshow.ItemPaths.ForEach(w => slideshowSerialized.Add($"Item{w.Item2}Path={w.Item1}"));
                    if (Slideshow.RssFeed != null) slideshowSerialized.Add($"{nameof(Slideshow.RssFeed)}={Slideshow.RssFeed}");
                    UpdateSection(Slideshow.Section.Item1, slideshowSerialized);
                }
                else
                {
                    RemoveSection(Slideshow.Section.Item1);
                }
            }

            try
            {
                // file integrity checking
                new FileInfo(ThemeFilePath).Directory.Create();
                VerifyFile();
                Logger.Trace($"theme file dump: {string.Join("\n", ThemeFileContent)}");
                Logger.Trace($"saving {ThemeFilePath}");
                File.WriteAllLines(ThemeFilePath, ThemeFileContent, Encoding);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not save theme file: ");
            }
        }

        public void SetContentAndParse(List<string> newContent)
        {
            ThemeFileContent = newContent;
            Parse();
        }

        /// <summary>
        /// Synchronizes the currently active Windows theme with Auto Dark Mode
        /// </summary>
        /// <param name="patch">Set to true if the theme switch fix should be applied or not. Set this to false if you plan to apply the active theme. <br/>
        /// Otherwise you might face theme state desync with Windows 11 22H2 and get a buggy taskbar / explorer.</param>
        /// <param name="keepDisplayNameAndGuid">if the name and guid of the original theme should be preserved</param>
        public void SyncWithActiveTheme(bool patch, bool keepDisplayNameAndGuid = false, bool logging = true)
        {
            try
            {
                string customPath = Path.Combine(Helper.PathThemeFolder, "Custom.theme");

                // call first becaues it refreshes the regkey
                (bool isCustom, string activeThemeName) = Tm2Handler.GetActiveThemeName();
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes");
                string themePath = (string)key.GetValue("CurrentTheme") ?? customPath;

                List<string> lines = new();
                string newDisplayName = null;
                if (themePath.Length > 0)
                {
                    (lines, Encoding, newDisplayName) = GetDisplayNameFromRaw(themePath);
                }
                else
                {
                    Logger.Warn("theme file path registry key empty, using custom theme");
                    themePath = customPath;
                    (Encoding, lines) = Read(themePath);
                    isCustom = true;
                }

                if (isCustom)
                {
                    if (logging) Logger.Info($"theme used for synching is custom theme, path: {themePath}");
                }
                else if (newDisplayName != null && newDisplayName.StartsWith("@%SystemRoot%\\System32\\themeui.dll"))
                {
                    if (logging) Logger.Info($"theme used for syncing is default theme with localized name, path: {themePath}");
                }
                else if (newDisplayName == null || newDisplayName != activeThemeName)
                {
                    if (logging) Logger.Info($"theme used for syncing has wrong name, using custom theme. expected: {activeThemeName} actual: {newDisplayName} with path: {themePath}");
                    themePath = customPath;
                    (Encoding, lines) = Read(themePath);
                }
                else
                {
                    if (logging) Logger.Info($"theme used for syncing is {newDisplayName}, path: {themePath}");
                }

                ThemeFileContent = lines;

                // save before parsing, because parsing updates the colors object
                string oldInfoText = Colors.InfoText.Item1;

                Parse();

                // ensure theme switching works properly in Win11 22H2. This is monumentally stupid but it seems to work.
                if (patch)
                {
                    if (Colors.InfoText.Item1 == oldInfoText) PatchColorsWin11InMemory(this);
                    else
                    {
                        Logger.Trace($"no color patch necessary. target theme: [{Colors.InfoText.Item1}], we have: [{oldInfoText}]");
                    }
                }

                if (!keepDisplayNameAndGuid)
                {
                    DisplayName = "ADMTheme";
                    ThemeId = $"{{{Guid.NewGuid()}}}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not sync theme file at {ThemeFilePath}, using default values: ");
            }
        }

        private void VerifyFile()
        {
            for (int i = 0; i < ThemeFileContent.Count; i++)
            {
                string line = ThemeFileContent[i];
                // removes invalid entries
                if (line.Length != 0 && !line.Contains('=') && !line.StartsWith('[') && !line.StartsWith(';'))
                {
                    Logger.Warn($"invalid ini file entry detected: {line}, removing line to preserve theme file integrity");
                    ThemeFileContent.RemoveAt(i);
                    i--;
                }
                // removes invalid sections
                else if (line.Length != 0 && line.StartsWith('[') && !line.EndsWith(']'))
                {
                    Logger.Warn($"invalid section detected: {line}, removing section to preserve theme file integrity");
                    line += "]";
                    ThemeFileContent[i] = line;
                    RemoveSection(line);
                }
                // removes spaces below a section header
                else if (line.Length != 0 && line.StartsWith('['))
                {
                    if ((i + 1) < ThemeFileContent.Count && ThemeFileContent[i + 1].Length == 0)
                    {
                        ThemeFileContent.RemoveAt(i + 1);
                        i--;
                    }
                }
                // removes spaces between elements
                else if (line.Length != 0 && (i + 2) < ThemeFileContent.Count)
                {
                    if (ThemeFileContent[i + 1].Length == 0)
                    {
                        if (!(ThemeFileContent[i + 2].StartsWith('[') || ThemeFileContent[i + 2].StartsWith(';')))
                        {
                            ThemeFileContent.RemoveAt(i + 1);
                            i--;
                        }
                    }
                }
            }
        }

        private static List<string> GetClassFieldsAndValues(object obj)
        {
            var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            List<string> props = obj.GetType().GetProperties(flags)
            .OrderBy(p =>
            {
                (string, int) propValue = ((string, int))p.GetValue(obj);
                return propValue.Item2;
            })
            .Select(p =>
            {
                (string, int) propValue = ((string, int))p.GetValue(obj);
                if (p.Name == "Section" || p.Name == "Description") return propValue.Item1;
                else return $"{p.Name}={propValue.Item1}";
            })
            .ToList();
            return props;
        }

        private static (Encoding, List<string>) Read(string ThemeFilePath, int attempts = 5)
        {
            List<string> themeFileContent = new();
            Encoding encoding = Encoding.GetEncoding(1252);
            try
            {
                themeFileContent = File.ReadAllLines(ThemeFilePath, encoding).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not read theme file at {ThemeFilePath}, using default values: ");
            }
            return (encoding, themeFileContent);
        }

        [GeneratedRegex("^Wallpaper([0-9]+)=")]
        private static partial Regex WallpaperRegex();

        private void Parse()
        {
            Desktop = new();
            VisualStyles = new();
            Cursors = new();
            Colors = new();
            Slideshow = new();

            Logger.Trace($"theme file dump: {string.Join("\n", ThemeFileContent)}");

            var iter = ThemeFileContent.GetEnumerator();
            bool processLastIterValue = false;
            /* processLastIterValue ensures that new sections are parsed properly
             * If it were not set, then the sections starting with [ would be discarded instead of re-processed at the start of the loop
             * Due to lazy evaluation, iter.MoveNext() will not be called in such instances
            /*/
            while (processLastIterValue || iter.MoveNext())
            {
                processLastIterValue = false;
                if (iter.Current.Contains("[Theme]"))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        if (iter.Current.Contains("DisplayName")) DisplayName = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("ThemeId")) ThemeId = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("UnmanagedOriginalName")) UnmanagedOriginalName = iter.Current.Split('=')[1].Trim();
                    }
                }
                else if (iter.Current.Contains(Desktop.Section.Item1))
                {
                    Regex wallpaperRegex = WallpaperRegex();
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        if (iter.Current.StartsWith("Wallpaper=")) Desktop.Wallpaper = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.StartsWith("Pattern=")) Desktop.Pattern = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.StartsWith("PicturePosition="))
                        {
                            if (int.TryParse(iter.Current.Split('=')[1].Trim(), out int pos))
                            {
                                Desktop.PicturePosition = pos;
                            }
                        }
                        else if (iter.Current.StartsWith("WindowsSpotlight="))
                        {
                            if (int.TryParse(iter.Current.Split('=')[1].Trim(), out int enabled))
                            {
                                Desktop.WindowsSpotlight = enabled;
                            }
                        }
                        else if (iter.Current.StartsWith("MultimonBackgrounds="))
                        {
                            bool success = int.TryParse(iter.Current.Split('=')[1].Trim(), out int num);
                            if (success) Desktop.MultimonBackgrounds = num;
                        }
                        else if (wallpaperRegex.Matches(iter.Current).Count > 0)
                        {
                            string[] split = iter.Current.Split('=');
                            Desktop.MultimonWallpapers.Add((split[1], split[0].Replace("Wallpaper", "")));
                        }
                    }
                }
                else if (iter.Current.Contains(VisualStyles.Section.Item1))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        SetValues(iter.Current, VisualStyles);
                    }
                }
                else if (iter.Current.Contains(Cursors.Section.Item1))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        SetValues(iter.Current, Cursors);
                    }
                }
                else if (iter.Current.Contains(Colors.Section.Item1))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        SetValues(iter.Current, Colors);
                    }
                }
                else if (iter.Current.Contains(Slideshow.Section.Item1))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }

                        Slideshow.Enabled = true;

                        if (iter.Current.Contains("ImagesRootPath")) Slideshow.ImagesRootPath = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("RssFeed")) Slideshow.RssFeed = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("ImagesRootPIDL")) Slideshow.ImagesRootPIDL = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("Interval"))
                        {
                            if (int.TryParse(iter.Current.Split('=')[1].Trim(), out int interval))
                            {
                                Slideshow.Interval = interval;
                            }
                        }
                        else if (iter.Current.Contains("Shuffle"))
                        {
                            bool success = int.TryParse(iter.Current.Split('=')[1].Trim(), out int num);
                            if (success) Slideshow.Shuffle = num;
                        }
                        else if (iter.Current.Contains("Item"))
                        {
                            string[] split = iter.Current.Split('=');
                            string itemNumber = split[0].Replace("Item", "").Replace("Path", "");
                            Slideshow.ItemPaths.Add((split[1], itemNumber));
                        }
                    }
                }
            }
        }

        private void RemoveKey(string section, string key)
        {
            try
            {
                int found = ThemeFileContent.IndexOf(section);
                int keyIdx = -1;
                if (found != -1)
                {
                    for (int i = found + 1; i < ThemeFileContent.Count; i++)
                    {
                        if (ThemeFileContent[i].StartsWith('[')) break;
                        else if (ThemeFileContent[i].StartsWith(key))
                        {
                            keyIdx = i;
                            break;
                        }
                    }
                }
                if (keyIdx != -1) ThemeFileContent.RemoveAt(keyIdx);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"failed to remove key {section}/{key}, exception: ");
                throw;
            }
        }

        private void RemoveSection(string section)
        {
            try
            {
                int found = ThemeFileContent.IndexOf(section);
                if (found != -1)
                {
                    int i;
                    for (i = found + 1; i < ThemeFileContent.Count; i++)
                    {
                        if (ThemeFileContent[i].StartsWith('[') && ThemeFileContent[i].EndsWith(']'))
                        {
                            break;
                        }
                    }
                    ThemeFileContent.RemoveRange(found, i - found);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"failed to remove section {section}\n exception: ");
                throw;
            }
        }

        private void UpdateSection(string section, List<string> lines)
        {
            try
            {
                int found = ThemeFileContent.IndexOf(section);
                if (found != -1)
                {
                    int i;
                    for (i = found + 1; i < ThemeFileContent.Count; i++)
                    {
                        if (ThemeFileContent[i].StartsWith('['))
                        {
                            break;
                        }
                    }
                    ThemeFileContent.RemoveRange(found, i - found);
                    lines.Add("");
                    ThemeFileContent.InsertRange(found, lines);
                }
                else
                {
                    ThemeFileContent.Add("");
                    ThemeFileContent.AddRange(lines);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"failed to update section {section} with data: {string.Join('\n', lines)}\n exception: ");
                throw;
            }
        }

        private static void SetValues(string input, object obj)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo p in obj.GetType().GetProperties(flags))
            {
                try
                {
                    if (p.Name == "Enabled") continue;
                    (string, int) propValue = ((string, int))p.GetValue(obj);
                    if (input.StartsWith(p.Name))
                    {
                        propValue.Item1 = input.Split('=')[1].Trim();
                        p.SetValue(obj, propValue);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not set value for input: {input}, exception: ");
                }
            }
        }

        /// <summary>
        /// Updates a value for a given key within a section. <br/>
        /// If the value doesn't exist, it will be added at the bottom of the section instead.
        /// </summary>
        /// <param name="section">The ini file section name</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void UpdateValue(string section, string key, string value)
        {
            try
            {
                int found = ThemeFileContent.IndexOf(section);
                if (found != -1)
                {
                    bool updated = false;
                    for (int i = found + 1; i < ThemeFileContent.Count; i++)
                    {
                        if (ThemeFileContent[i].StartsWith('[')) break;
                        else if (ThemeFileContent[i].StartsWith(key))
                        {
                            ThemeFileContent[i] = $"{key}={value}";
                            updated = true;
                            break;
                        }
                    }
                    if (!updated)
                    {
                        ThemeFileContent.Insert(found + 1, $"{key}={value}");
                    }
                }
                else
                {
                    ThemeFileContent.Add("");
                    ThemeFileContent.Add(section);
                    ThemeFileContent.Add($"{key}={value}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"failed to update value {section}/{key} with value {value}, exception: ");
                throw;
            }
        }
    }
}