using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AutoDarkModeSvc.Handlers.IThemeManager.Interfaces;

namespace AutoDarkModeSvc.Handlers.IThemeManager;

/*
 * Source: https://github.com/kuchienkz/KAWAII-Theme-Swithcer/blob/master/KAWAII%20Theme%20Switcher/KAWAII%20Theme%20Helper.cs
 * Originally created by Kuchienkz.
 * Email: wahyu.darkflame@gmail.com
 * Licensed under: GNU General Public License v3.0
 * 
 * Other Contributors (modified by):
 * Armin2208
 * Spiritreader
*/

internal class TmHandler
{
    [ComImport, Guid("C04B329E-5823-4415-9C93-BA44688947B0"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
    public class ThemeManagerClass : Interfaces.IThemeManager, ThemeManager
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void ApplyTheme([In, MarshalAs(UnmanagedType.BStr)] string bstrThemePath);
        [DispId(0x60010000)]
        public virtual extern ITheme CurrentTheme
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            get;
        }
    }
    private static class NativeMethods
    {
        [DllImport("UxTheme.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsThemeActive();
    }

    public static string GetThemeStatus()
    {
        return NativeMethods.IsThemeActive() ? "running" : "stopped";
    }
}
