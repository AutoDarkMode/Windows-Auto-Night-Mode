using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Core;

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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly GlobalState state = GlobalState.Instance();

        /// <summary>
        /// Applies the theme using the KAWAII Theme switcher logic for windows theme files
        /// </summary>
        /// <param name="config"></param>
        /// <param name="newTheme"></param>
        /// <param name="automatic"></param>
        /// <param name="sunset"></param>
        /// <param name="sunrise"></param>
        /// <returns>true if an update was performed; false otherwise</returns>
        public static bool ApplyTheme(AdmConfig config, Theme newTheme)
        {
            if (config.WindowsThemeMode.DarkThemePath == null || config.WindowsThemeMode.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return false;
            }
            if (!File.Exists(config.WindowsThemeMode.DarkThemePath))
            {
                Logger.Error($"invalid dark theme path: {config.WindowsThemeMode.DarkThemePath}");
                return false;
            }
            if (!File.Exists(config.WindowsThemeMode.LightThemePath))
            {
                Logger.Error($"invalid light theme path: {config.WindowsThemeMode.LightThemePath}");
                return false;
            }
            if (!config.WindowsThemeMode.DarkThemePath.EndsWith(".theme") || !config.WindowsThemeMode.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return false;
            }

            // TODO change tracking when having active theme monitor disabled
            if (newTheme == Theme.Dark && !state.CurrentWindowsThemeName.Equals(Path.GetFileNameWithoutExtension(config.WindowsThemeMode.DarkThemePath), StringComparison.Ordinal))
            {
                PowerHandler.RequestDisableEnergySaver(config);
                Apply(config.WindowsThemeMode.DarkThemePath);
                return true;
            }
            else if (newTheme == Theme.Light && !state.CurrentWindowsThemeName.Equals(Path.GetFileNameWithoutExtension(config.WindowsThemeMode.LightThemePath), StringComparison.Ordinal))
            {
                PowerHandler.RequestDisableEnergySaver(config);
                Apply(config.WindowsThemeMode.LightThemePath);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void SyncCustomThemeToDisk()
        {
            FileSystemWatcher watcher = new();
            ManualResetEvent interrupt = new(false);
            try
            {
                Logger.Debug("refreshing Custom.theme values");
                ThemeFile custom = new(Path.Combine(Extensions.ThemeFolderPath, "Custom.theme"));
                watcher.Path = Extensions.ThemeFolderPath;
                watcher.NotifyFilter = NotifyFilters.Attributes |
                    NotifyFilters.CreationTime |
                    NotifyFilters.DirectoryName |
                    NotifyFilters.FileName |
                    NotifyFilters.LastAccess |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Security |
                    NotifyFilters.Size;
                watcher.Filter = "Custom.theme";

                Thread cancellation = new(() =>
                {
                    try
                    {
                        if (!interrupt.WaitOne(TimeSpan.FromMilliseconds(5000))) {
                            Logger.Error("theme update timeout, couldn't refresh custom theme, settings may desync");
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logger.Debug("aborting cancellation procedure due to thread interrupt");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "error while waiting for timeout: ");
                    }
                });
                cancellation.Start();

                watcher.Changed += new((object source, FileSystemEventArgs e) =>
                {
                    Logger.Debug("Custom.theme modified");
                    ThemeFile customRefreshed = new(Path.Combine(Extensions.ThemeFolderPath, "Custom.theme"));
                    if (customRefreshed.ThemeId != custom.ThemeId)
                    {
                        Logger.Debug("windows Custom.theme write detected, refreshing theme");
                        Apply(Path.Combine(Extensions.ThemeFolderPath, "Custom.theme"), suppressLogging: true);
                        interrupt.Set();
                    }
                });
                watcher.EnableRaisingEvents = true;
                custom.RefreshGuid();
                custom.Save();
                cancellation.Join();

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't refresh custom theme, settings may desync");
            }
            finally
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                interrupt.Dispose();
            }
        }
        public static void ApplyManagedTheme(AdmConfig config, string path)
        {
            PowerHandler.RequestDisableEnergySaver(config);
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
            return new ThemeManagerClass().CurrentTheme.DisplayName;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Apply(string themeFilePath, bool suppressLogging = false)
        {
            /*Exception applyEx = null;*/
            Thread thread = new(() =>
            {
                try
                {
                    new ThemeManagerClass().ApplyTheme(themeFilePath);
                    state.CurrentWindowsThemeName = Path.GetFileNameWithoutExtension(themeFilePath);
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
            /*
            if (applyEx != null)
            {
                throw applyEx;
            }
            */
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
                && state.CurrentWindowsThemeName == themePath)
            {
                Logger.Debug("enforcing theme refresh with disabled MonitorActiveTheme");
                state.CurrentWindowsThemeName = "";
            }
        }
    }
}