using AutoDarkModeSvc.Handlers.ThemeFiles;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers
{
    internal class ThemeDllHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static Dictionary<string, string> LearnedThemeNames { get; } = new();

        [DllImport("ThemeDll.dll")]
        private static extern int themetool_init();

        [DllImport("ThemeDll.dll")]
        private static extern int themetool_set_active(int hWnd, ulong theme_idx, bool apply_now_not_only_registry, ulong apply_flags, ulong pack_flags);

        [DllImport("ThemeDll.dll")]
        private static extern int themetool_get_theme_count(out ulong count);
        [DllImport("ThemeDll.dll")]
        private static extern int themetool_theme_get_display_name(IntPtr theme, IntPtr outPtr, int size);
        [DllImport("ThemeDll.dll")]
        private static extern int themetool_get_theme(ulong idx, out IntPtr theme);
        [DllImport("ThemeDll.dll")]
        private static extern void themetool_theme_release(IntPtr theme);

        private static bool initialized = false;

        private static object _lock = new();

        private static bool InitThemeManager()
        {
            lock(_lock)
            {
                if (initialized) return true;
                Logger.Debug("initializing IThemeManger2");
                try
                {
                    int res = themetool_init();
                    if (res == 0)
                    {
                        initialized = true;
                        return true;
                    }
                    else throw new ExternalException($"StatusCode {res}", res);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not initialize IThemeManager2: ");
                }
            }

            return false;
        }

        public static bool SetTheme(Theme2Wrapper theme)
        {
            if (!initialized)
            {
                InitThemeManager();
            }
            try
            {
                int res = themetool_set_active(0, (ulong)theme.idx, true, 0, 0);
                Logger.Info($"applied theme {theme.ThemeName} successfully via IThemeManager2");
                if (res != 0)
                {
                    throw new ExternalException($"StatusCode: {res}", res);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not apply theme:");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets a theme given a path
        /// </summary>
        /// <param name="path">the path of the theme file</param>
        /// <returns>the first tuple entry is true if the theme was found, the second is true if theme switching was successful</returns>
        public static (bool, bool) SetTheme(string displayName)
        {
            if (displayName == null)
            {
                return (false, false);
            }

            if (LearnedThemeNames.ContainsKey(displayName))
            {
                displayName = LearnedThemeNames[displayName];
            }

            if (!initialized)
            {
                InitThemeManager();
            }

            List<Theme2Wrapper> themes = GetThemeList();

            if (themes.Count > 0)
            {
                Theme2Wrapper targetTheme = themes.Where(t => t.ThemeName == displayName).FirstOrDefault();
                if (targetTheme != null)
                {
                    return (true, SetTheme(targetTheme));
                }
            }
            return (false, false);
        }

        public static List<Theme2Wrapper> GetThemeList()
        {
            if (!initialized)
            {
                InitThemeManager();
            }
            List<Theme2Wrapper> list = new();
            ulong uCount;
            try
            {
                int res = themetool_get_theme_count(out uCount);
                if (res != 0)
                {
                    return new();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error getting theme count from IThemeManager2:");
                return list;
            }

            int count = Convert.ToInt32(uCount);
            for (int i = 0; i < count; i++)
            {
                try
                {
                    themetool_get_theme((ulong)i, out IntPtr theme);
                    IntPtr ptr = Marshal.AllocCoTaskMem(IntPtr.Size * 256);
                    themetool_theme_get_display_name(theme, ptr, 256);
                    
                    //omit for now, entry point missing
                    //themetool_theme_release(theme);
                    string name = "";
                    if (ptr != IntPtr.Zero)
                    {
                        name = Marshal.PtrToStringUni(ptr);
                        Marshal.FreeCoTaskMem(ptr);
                    }

                    list.Add(new()
                    {
                        idx = i,
                        ThemeName = name
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"error getting theme data for id {i} from IThemeManager2:");
                }
            }
            return list;
        }
    }

    public class Theme2Wrapper
    {
        public string ThemeName { get; set; }
        public int idx { get; set; }
    }
}
