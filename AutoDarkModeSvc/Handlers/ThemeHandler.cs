using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Core;
using System.Threading.Tasks;
using AutoDarkModeLib.Configs;
using System.Collections.Generic;
using System.Diagnostics;
using AutoDarkModeSvc.Communication;

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

namespace AutoDarkModeSvc.Handlers
{
    public static class ThemeHandler
    {
        private static readonly object _syncRoot = new();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly GlobalState state = GlobalState.Instance();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        public static bool ThemeModeNeedsUpdate(Theme newTheme, bool skipCheck = false) {
            if (builder.Config.WindowsThemeMode.DarkThemePath == null || builder.Config.WindowsThemeMode.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return false;
            }
            if (!File.Exists(builder.Config.WindowsThemeMode.DarkThemePath))
            {
                Logger.Error($"invalid dark theme path: {builder.Config.WindowsThemeMode.DarkThemePath}");
                return false;
            }
            if (!File.Exists(builder.Config.WindowsThemeMode.LightThemePath))
            {
                Logger.Error($"invalid light theme path: {builder.Config.WindowsThemeMode.LightThemePath}");
                return false;
            }
            if (!builder.Config.WindowsThemeMode.DarkThemePath.EndsWith(".theme") || !builder.Config.WindowsThemeMode.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return false;
            }

            // TODO change tracking when having active theme monitor disabled
            if (newTheme == Theme.Dark && (skipCheck ||
                !state.UnmanagedActiveThemePath.Equals(builder.Config.WindowsThemeMode.DarkThemePath, StringComparison.Ordinal)))
            {
                return true;
            }
            else if (newTheme == Theme.Light && (skipCheck ||
                !state.UnmanagedActiveThemePath.Equals(builder.Config.WindowsThemeMode.LightThemePath, StringComparison.Ordinal)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies the theme using the KAWAII Theme switcher logic for windows theme files
        /// </summary>
        /// <param name="config"></param>
        /// <param name="newTheme"></param>
        /// <param name="automatic"></param>
        /// <param name="sunset"></param>
        /// <param name="sunrise"></param>
        /// <returns>true if an update was performed; false otherwise</returns>
        public static void ApplyTheme(Theme newTheme)
        {
            PowerHandler.RequestDisableEnergySaver(builder.Config);
            if (builder.Config.WindowsThemeMode.MonitorActiveTheme)
            {
                WindowsThemeMonitor.PauseThemeMonitor(TimeSpan.FromSeconds(10));
            }
            if (newTheme == Theme.Light) Apply(builder.Config.WindowsThemeMode.LightThemePath);
            else if (newTheme == Theme.Dark) Apply(builder.Config.WindowsThemeMode.DarkThemePath);
        }

        public static void ApplyManagedTheme(AdmConfig config, string path)
        {
            Apply(path);
        }

        [ComImport, Guid("D23CC733-5522-406D-8DFB-B3CF5EF52A71"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITheme
        {
            [DispId(0x60010000)]
            string DisplayName
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
            [DispId(0x60010001)]
            string VisualStyle
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
        }
        [ComImport, Guid("0646EBBE-C1B7-4045-8FD0-FFD65D3FC792"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IThemeManager
        {
            [DispId(0x60010000)]
            ITheme CurrentTheme
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                get;
            }
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ApplyTheme([In, MarshalAs(UnmanagedType.BStr)] string bstrThemePath);
        }
        [ComImport, Guid("A2C56C2A-E63A-433E-9953-92E94F0122EA"), CoClass(typeof(ThemeManagerClass))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public interface ThemeManager : IThemeManager { }
        [ComImport, Guid("C04B329E-5823-4415-9C93-BA44688947B0"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
        public class ThemeManagerClass : IThemeManager, ThemeManager
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
        public static string GetCurrentThemeName()
        {
            string themeName = "";/*Exception applyEx = null;*/
            Thread thread = new(() =>
            {
                try
                {
                    themeName = new ThemeManagerClass().CurrentTheme.DisplayName;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not retrieve active theme name");
                    //applyEx = ex;
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }
            return themeName;
        }

        private static void ApplyIThemeManager(string themeFilePath, bool suppressLogging = false)
        {
            /*Exception applyEx = null;*/
            Thread thread = new(() =>
            {
                try
                {
                    new ThemeManagerClass().ApplyTheme(themeFilePath);
                    state.UnmanagedActiveThemePath = themeFilePath;
                    if (!suppressLogging) Logger.Info($"applied theme \"{themeFilePath}\" successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not apply theme \"{themeFilePath}\"");
                    //applyEx = ex;
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }
        }

        private static void ApplyIThemeManager2(string themeFilePath, bool suppressLogging = false)
        {
            DateTime start = DateTime.Now;
            /*Exception applyEx = null;*/
            Thread thread = new(() =>
            {
                bool tm2Found = false;
                bool tm2Success = false;
                string displayNameFromFile = null;
                try
                {
                    (_, displayNameFromFile) = ThemeFile.GetDisplayNameFromRaw(themeFilePath);


                    (tm2Found, tm2Success) = ThemeDllHandler.SetThemeViaBridge(displayNameFromFile);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not retrieve display name for path {themeFilePath}:");
                }

                if (tm2Success && tm2Found)
                {
                    state.UnmanagedActiveThemePath = themeFilePath;
                    return;
                }

                if (!tm2Found)
                {
                    Logger.Warn($"could not find theme for display name {displayNameFromFile}, using IThemeManager mitigation");
                }

                bool tm1Success = false;

                try
                {
                    new ThemeManagerClass().ApplyTheme(themeFilePath);
                    state.UnmanagedActiveThemePath = themeFilePath;
                    if (!suppressLogging) Logger.Info($"applied theme \"{themeFilePath}\" successfully via IThemeManager");
                    tm1Success = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not apply theme \"{themeFilePath}\"");
                    //applyEx = ex;
                }

                if (tm1Success && !tm2Found)
                {
                    string displayNameApi = GetCurrentThemeName();
                    Logger.Debug($"renewed display name: {displayNameApi}");
                    (tm2Found, tm2Success) = ThemeDllHandler.SetThemeViaBridge(displayNameApi);
                    if (!tm2Found)
                    {
                        Logger.Error("failed to find target theme after IThemeManager application");
                    }
                    else
                    {
                        if (!state.LearnedThemeNames.ContainsKey(displayNameFromFile))
                        {
                            Logger.Debug($"learnt new theme name association: {displayNameFromFile}={displayNameApi}");
                            state.LearnedThemeNames.Add(displayNameFromFile, displayNameApi);
                        }
                        else
                        {
                            Logger.Debug($"updated theme name association: {displayNameFromFile}={displayNameApi}");
                            state.LearnedThemeNames[displayNameFromFile] = displayNameApi;
                        }
                    }
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }
            DateTime end = DateTime.Now;

            TimeSpan elapsed = end - start;

            if (elapsed.TotalSeconds > 10)
            {
                Logger.Warn($"theme switching took longer than expected ({elapsed.TotalSeconds} seconds)");
            }
        }

        public static void Apply(string themeFilePath, bool suppressLogging = false)
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_22H2)
            {
                ApplyIThemeManager2(themeFilePath, suppressLogging);
            }
            else
            {
                ApplyIThemeManager(themeFilePath, suppressLogging);
            }
        }

        public static string GetCurrentVisualStyleName()
        {
            return Path.GetFileName(new ThemeManagerClass().CurrentTheme.VisualStyle);
        }

        public static string GetThemeStatus()
        {
            return NativeMethods.IsThemeActive() ? "running" : "stopped";
        }

        /// <summary>
        /// Forces the theme to update when the automatic theme detection is disabled
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="state"></param>
        /// <param name="theme"></param>
        public static void EnforceNoMonitorUpdates(AdmConfigBuilder builder, GlobalState state, Theme theme)
        {
            string themePath = "";
            switch (theme)
            {
                case Theme.Light:
                    themePath = Path.GetFileNameWithoutExtension(builder.Config.WindowsThemeMode.LightThemePath);
                    break;
                case Theme.Dark:
                    themePath = Path.GetFileNameWithoutExtension(builder.Config.WindowsThemeMode.DarkThemePath);
                    break;
            }
            if (builder.Config.WindowsThemeMode.Enabled
                && !builder.Config.WindowsThemeMode.MonitorActiveTheme
                && state.UnmanagedActiveThemePath == themePath)
            {
                Logger.Debug("enforcing theme refresh with disabled MonitorActiveTheme");
                state.UnmanagedActiveThemePath = "";
            }
        }
    }
}