using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers.ThemeFiles
{
    internal class ThemeFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string ThemeFilePath { get; set; }
        public List<string> ThemeFileLoaded { get; private set; } = new();
        public string DisplayName { get; set; }
        public string ThemeId { get; set; } = $"{{{Guid.NewGuid()}}}";

        public Desktop Desktop { get; set; } = new();
        public VisualStyles VisualStyles { get; set; } = new();
        public Cursors Cursors { get; set; } = new();

        public ThemeFile(string path)
        {
            ThemeFilePath = path;
            Load();
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

        private void UpdateValue(string section, string key)
        {

        }
        private void UpdateSection(string section, object obj)
        {
            int found = ThemeFileLoaded.IndexOf(section);
            if (found != -1)
            {
                int i;
                for (i = found + 1; i < ThemeFileLoaded.Count; i++)
                {
                    if (ThemeFileLoaded[i].StartsWith('['))
                    {
                        i--;
                        break;
                    }
                }
                ThemeFileLoaded.RemoveRange(found, i - found);
                ThemeFileLoaded.InsertRange(found, GetClassFieldsAndValues(obj));
            }
            else
            {
                ThemeFileLoaded.AddRange(GetClassFieldsAndValues(obj));
            }
        }

        public void Save()
        {
            UpdateSection(Cursors.Section.Item1, Cursors);
            UpdateSection(VisualStyles.Section.Item1, VisualStyles);
        }

        public void Load()
        {
            ThemeFileLoaded = System.IO.File.ReadAllLines(ThemeFilePath, Encoding.GetEncoding(1252)).ToList();
            var iter = ThemeFileLoaded.GetEnumerator();
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
                        else if (iter.Current.Contains("MultimonBackgrounds"))
                        {
                            bool success = int.TryParse(iter.Current.Split('=')[1].Trim(), out int num);
                            if (success) Desktop.MultimonBackgrounds = num;
                        }
                        else if (iter.Current.Contains("Wallpaper"))
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
