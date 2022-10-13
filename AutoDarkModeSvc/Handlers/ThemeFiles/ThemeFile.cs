using AutoDarkModeLib;
using AutoDarkModeSvc.Monitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    public class ThemeFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string ThemeFilePath { get; private set; }
        public List<string> ThemeFileContent { get; private set; } = new();
        public string DisplayName { get; set; } = "ADMTheme";
        public string ThemeId { get; set; } = $"{{{Guid.NewGuid()}}}";
        public MasterThemeSelector MasterThemeSelector { get; set; } = new();
        public Desktop Desktop { get; set; } = new();
        public VisualStyles VisualStyles { get; set; } = new();
        public Cursors Cursors { get; set; } = new();
        public Colors Colors { get; set; } = new();
        public Slideshow Slideshow { get; set; } = new();

        public ThemeFile(string path)
        {
            ThemeFilePath = path;
        }

        public void RefreshGuid()
        {
            ThemeId = $"{{{Guid.NewGuid()}}}";
        }

        public static List<string> GetClassFieldsAndValues(object obj)
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
                            i--;
                            break;
                        }
                    }
                    ThemeFileContent.RemoveRange(found, i - found);
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
                        if (ThemeFileContent[i].StartsWith('['))
                        {
                            i--;
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

        public void Save()
        {
            UpdateValue("[Theme]", nameof(ThemeId), ThemeId);
            UpdateValue("[Theme]", nameof(DisplayName), DisplayName);

            UpdateSection(Cursors.Section.Item1, GetClassFieldsAndValues(Cursors));
            UpdateSection(VisualStyles.Section.Item1, GetClassFieldsAndValues(VisualStyles));
            UpdateValue(Colors.Section.Item1, nameof(Colors.Background), Colors.Background.Item1);

            UpdateValue(MasterThemeSelector.Section.Item1, nameof(MasterThemeSelector.MTSM), MasterThemeSelector.MTSM);

            //Update Desktop class manually due to the way it is internally represented
            List<string> desktopSerialized = new();
            desktopSerialized.Add(Desktop.Section.Item1);
            desktopSerialized.Add($"{nameof(Desktop.Wallpaper)}={Desktop.Wallpaper}");
            desktopSerialized.Add($"{nameof(Desktop.Pattern)}={Desktop.Pattern}");
            desktopSerialized.Add($"{nameof(Desktop.MultimonBackgrounds)}={Desktop.MultimonBackgrounds}");
            desktopSerialized.Add($"{nameof(Desktop.PicturePosition)}={Desktop.PicturePosition}");
            Desktop.MultimonWallpapers.ForEach(w => desktopSerialized.Add($"Wallpaper{w.Item2}={w.Item1}"));
            UpdateSection(Desktop.Section.Item1, desktopSerialized);

            //Update Slideshow
            if (Slideshow.Enabled)
            {
                List<string> slideshowSerialized = new();
                slideshowSerialized.Add(Slideshow.Section.Item1);
                slideshowSerialized.Add($"{nameof(Slideshow.Interval)}={Slideshow.Interval}");
                slideshowSerialized.Add($"{nameof(Slideshow.Shuffle)}={Slideshow.Shuffle}");
                if (Slideshow.ImagesRootPath != null) slideshowSerialized.Add($"{nameof(Slideshow.ImagesRootPath)}={Slideshow.ImagesRootPath}");
                if (Slideshow.ImagesRootPIDL != null) slideshowSerialized.Add($"{nameof(Slideshow.ImagesRootPIDL)}={Slideshow.ImagesRootPIDL}");
                Slideshow.ItemPaths.ForEach(w => slideshowSerialized.Add($"Item{w.Item2}Path={w.Item1}"));
                if (Slideshow.RssFeed != null) slideshowSerialized.Add($"{nameof(Slideshow.RssFeed)}={Slideshow.RssFeed}");
                UpdateSection(Slideshow.Section.Item1, slideshowSerialized);
            }


            try
            {
                new FileInfo(ThemeFilePath).Directory.Create();
                File.WriteAllLines(ThemeFilePath, ThemeFileContent, Encoding.GetEncoding(1252));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not save theme file: ");
            }
        }

        public void RemoveSlideshow()
        {
            Slideshow.Enabled = false;
            RemoveSection(Slideshow.Section.Item1);
        }

        private void Parse()
        {
            Desktop = new();
            VisualStyles = new();
            Cursors = new();
            Colors = new();
            Slideshow = new();

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
                    }
                }
                else if (iter.Current.Contains(Desktop.Section.Item1))
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.StartsWith("["))
                        {
                            processLastIterValue = true;
                            break;
                        }
                        if (iter.Current.Contains("Wallpaper=")) Desktop.Wallpaper = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("Pattern")) Desktop.Pattern = iter.Current.Split('=')[1].Trim();
                        else if (iter.Current.Contains("PicturePosition"))
                        {
                            if (int.TryParse(iter.Current.Split('=')[1].Trim(), out int pos)) 
                            {
                                Desktop.PicturePosition = pos;
                            }
                        }
                        else if (iter.Current.Contains("MultimonBackgrounds"))
                        {
                            bool success = int.TryParse(iter.Current.Split('=')[1].Trim(), out int num);
                            if (success) Desktop.MultimonBackgrounds = num;
                        }
                        else if (iter.Current.Contains("Wallpaper") && !iter.Current.Contains("WallpaperWriteTime"))
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

        public void Set(List<string> newContent)
        {
            string tempPath = ThemeFilePath;
            ThemeFileContent = newContent;
            ThemeFilePath = tempPath;
            Parse();
        }

        public void Load()
        {
            try
            {
                ThemeFileContent = File.ReadAllLines(ThemeFilePath, Encoding.GetEncoding(1252)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not read theme file at {ThemeFilePath}, using default values: ");
            }
            Parse();
        }

        public void SyncActiveThemeData(bool keepDisplayNameAndGuid = false)
        {
            try
            {
                string themePath = RegistryHandler.GetActiveThemePath();
                if (themePath != "")
                {
                    ThemeFileContent = File.ReadAllLines(themePath, Encoding.GetEncoding(1252)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not sync theme file at {ThemeFilePath}, using default values: ");
            }
            Parse();
            if (!keepDisplayNameAndGuid)
            {
                DisplayName = "ADMTheme";
                ThemeId = $"{{{Guid.NewGuid()}}}";
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
    }

}
