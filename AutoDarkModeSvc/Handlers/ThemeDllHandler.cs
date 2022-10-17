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
            themetool_get_theme_count(out ulong uCount);

            int count = Convert.ToInt32(uCount);
            for (int i = 0; i < count; i++)
            {
                themetool_get_theme(7, out IntPtr theme);
                IntPtr ptr = IntPtr.Zero;
                ptr = Marshal.AllocCoTaskMem(IntPtr.Size * 256);
                themetool_theme_get_display_name(theme, ptr, 256);
                string name = "";
                if (ptr != IntPtr.Zero)
                {
                    name = Marshal.PtrToStringUni(ptr);
                    Marshal.FreeCoTaskMem(ptr);
                }
                
            }
            return new();
        }
    }

    public class Theme2Wrapper
    {
        public string ThemeName { get; set; }
        public int idx { get; set; }
    }
}
