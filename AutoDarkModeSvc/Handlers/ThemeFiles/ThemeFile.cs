using AutoDarkModeSvc.Monitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    public class ThemeFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string ThemeFilePath { get; set; }
        public List<string> ThemeFileContent { get; private set; } = new();
        public string DisplayName { get; set; } = "ADMTheme";
        public string ThemeId { get; set; } = $"{{{Guid.NewGuid()}}}";

        public Desktop Desktop { get; set; } = new();
        public VisualStyles VisualStyles { get; set; } = new();
        public Cursors Cursors { get; set; } = new();
        public Colors Colors { get; set; } = new();

        public ThemeFile(string path)
        {
            ThemeFilePath = path;
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
        private void UpdateSection(string section, List<string> lines)
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

        public void Save()
        {

            UpdateValue("[Theme]", nameof(ThemeId), ThemeId);
            UpdateValue("[Theme]", nameof(DisplayName), DisplayName);

            UpdateSection(Cursors.Section.Item1, GetClassFieldsAndValues(Cursors));
            UpdateSection(VisualStyles.Section.Item1, GetClassFieldsAndValues(VisualStyles));
            UpdateValue(Colors.Section.Item1, nameof(Colors.Background), Colors.Background.Item1);

            //Update Desktop class manually due to the way it is internally represented
            List<string> desktopSerialized = new();
            desktopSerialized.Add(Desktop.Section.Item1);
            desktopSerialized.Add($"{nameof(Desktop.Wallpaper)}={Desktop.Wallpaper}");
            desktopSerialized.Add($"{nameof(Desktop.Pattern)}={Desktop.Pattern}");
            desktopSerialized.Add($"{nameof(Desktop.MultimonBackgrounds)}={Desktop.MultimonBackgrounds}");
            desktopSerialized.Add($"{nameof(Desktop.PicturePosition)}={Desktop.PicturePosition}");
            Desktop.MultimonWallpapers.ForEach(w => desktopSerialized.Add($"Wallpaper{w.Item2}={w.Item1}"));
            UpdateSection(Desktop.Section.Item1, desktopSerialized);
            try
            {
                System.IO.File.WriteAllLines(ThemeFilePath, ThemeFileContent, Encoding.GetEncoding(1252));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not save theme file: ");
            }
        }

        public void Load()
        {
            try
            {
                ThemeFileContent = System.IO.File.ReadAllLines(RegistryHandler.GetActiveThemePath(), Encoding.GetEncoding(1252)).ToList();
                DisplayName = "ADMTheme";
                ThemeId = $"{{{Guid.NewGuid()}}}";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not read theme file at {ThemeFilePath}, using default values: ");
            }

            Desktop = new();
            VisualStyles = new();
            Cursors = new();
            Colors = new();

            var iter = ThemeFileContent.GetEnumerator();
            bool processLastIterValue = false;
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
                        // if (iter.Current.Contains("DisplayName")) DisplayName = iter.Current.Split('=')[1].Trim();
                        // else if (iter.Current.Contains("ThemeId")) ThemeId = iter.Current.Split('=')[1].Trim();
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
                        else if (iter.Current.Contains("MultimonBackgrounds"))
                        {
                            bool success = int.TryParse(iter.Current.Split('=')[1].Trim(), out int num);
                            if (success) Desktop.MultimonBackgrounds = num;
                        }
                        else if (iter.Current.Contains("Wallpaper") && !iter.Current.Equals("WallpaperWriteTime"))
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
            }
        }

        private static void SetValues(string input, object obj)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo p in obj.GetType().GetProperties(flags))
            {
                (string, int) propValue = ((string, int))p.GetValue(obj);
                if (input.StartsWith(p.Name))
                {
                    propValue.Item1 = input.Split('=')[1].Trim();
                    p.SetValue(obj, propValue);
                }
            }
        }
    }

}
