using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace AutoDarkModeSvc.Handlers
{
    static class WallpaperHandler
    {
        /// <summary>
        /// Sets the first background in a wallpaper collection
        /// </summary>
        /// <param name="wallpaperCollection">List with wallpapers</param>
        public static void SetBackground(ICollection<string> wallpaperCollection)
        {
            List<string> wallpapers = wallpaperCollection.ToList();
            if (wallpapers.Count > 0)
            {
                string filePath = wallpapers.ElementAt(0);
                win32.SystemParametersInfo(0x0014, 0, filePath, 1 | 2);
            }
        }

        /// <summary>
        /// Gets the currenvt wallpaper
        /// </summary>
        /// <returns>string with a path to the current wallpapers</returns>
        public static string GetBackground()
        {
            string currentWallpaper = new string('\0', 260);
            win32.SystemParametersInfo(0x0073, currentWallpaper.Length, currentWallpaper, 0);
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }

        internal sealed class win32
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int SystemParametersInfo(int uAction, int uParam, String lpvParam, int fuWinIni);
        }
    }
}
