using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AutoThemeChanger
{
    class DeskBGHandler
    {
        public DeskBGHandler()
        {
        }

        public static void SetBackground(string filePath)
        {

            win32.SystemParametersInfo(20, 0, filePath, 1 | 2);
        }

        public string GetBackground()
        {
            string currentWallpaper = new string('\0', 260);
            win32.SystemParametersInfo(0x73, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }

        internal sealed class win32
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int SystemParametersInfo(int uAction, int uParam, String lpvParam, int fuWinIni);
        }
    }
}
