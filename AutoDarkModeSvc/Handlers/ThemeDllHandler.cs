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

        public static bool InitThemeManager()
        {
            int res = themetool_init();
            if (res == 0)
            {
                return true;
            }
            return false;
        }

        public static List<Theme2Wrapper> GetThemeList()
        {
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

                    themetool_theme_release(theme);
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
