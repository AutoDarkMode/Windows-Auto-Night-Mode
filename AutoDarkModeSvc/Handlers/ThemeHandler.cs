#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Monitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static AutoDarkModeLib.IThemeManager2.Flags;
using static AutoDarkModeSvc.Handlers.IThemeManager.TmHandler;

namespace AutoDarkModeSvc.Handlers
{
    public static class ThemeHandler
    {
        private static readonly object _syncRoot = new();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly GlobalState state = GlobalState.Instance();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();

        private static void Apply(string themeFilePath, bool suppressLogging = false, ThemeFile unmanagedPatched = null, List<ThemeApplyFlags> flagList = null)
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                ApplyIThemeManager2(themeFilePath, suppressLogging, unmanagedPatched, flagList);
            }
            else
            {
                ApplyIThemeManager(themeFilePath, suppressLogging, unmanagedPatched);
            }
        }

        /// <summary>
        /// Applies an ADM managed theme
        /// </summary>
        /// <param name="theme">the theme file that should be applied in managed mode</param>
        /// <param name="flagList">the list of ignore flags that should be used when applying the theme</param>
        public static void ApplyManagedTheme(ThemeFile theme, List<ThemeApplyFlags> flagList)
        {
            Apply(theme.ThemeFilePath, flagList: flagList);
        }

        /// <summary>
        /// Applies an unmanaged theme to AutoDarkMode depending on whether the darkget theme is light or dark <br/>
        /// Will copy the data from the source theme into the ADM unmanaged theme and patch colors if required.
        /// </summary>
        /// <param name="newTheme"></param>
        /// <returns>true if an update was performed; false otherwise</returns>
        public static void ApplyUnmanagedTheme(Theme newTheme)
        {
            PowerHandler.RequestDisableEnergySaver(builder.Config);
            if (builder.Config.WindowsThemeMode.MonitorActiveTheme)
            {
                WindowsThemeMonitor.PauseThemeMonitor();
            }

            // string appliedThemeFilePath = null;

            // refresh active theme for syncing data into unmanaged themes
            state.ManagedThemeFile.SyncWithActiveTheme(patch: false, logging: false);

            if (newTheme == Theme.Light)
            {
                ThemeFile light = ThemeFile.LoadUnmanagedTheme(builder.Config.WindowsThemeMode.LightThemePath, Helper.PathUnmanagedLightTheme);
                light.UnmanagedOriginalName = light.DisplayName;
                light.DisplayName = Helper.NameUnmanagedLightTheme;
                if (light.Colors.InfoText.Item1 == state.ManagedThemeFile.Colors.InfoText.Item1)
                {
                    ThemeFile.PatchColorsWin11AndSave(light);
                }
                else
                {
                    light.Save(managed: false);
                }
                Apply(builder.Config.WindowsThemeMode.LightThemePath, unmanagedPatched: light);
            }
            else if (newTheme == Theme.Dark)
            {
                ThemeFile dark = ThemeFile.LoadUnmanagedTheme(builder.Config.WindowsThemeMode.DarkThemePath, Helper.PathUnmanagedDarkTheme);
                dark.UnmanagedOriginalName = dark.DisplayName;
                dark.DisplayName = Helper.NameUnmanagedDarkTheme;
                if (dark.Colors.InfoText.Item1 == state.ManagedThemeFile.Colors.InfoText.Item1)
                {
                    ThemeFile.PatchColorsWin11AndSave(dark);
                }
                else
                {
                    dark.Save(managed: false);
                }
                Apply(builder.Config.WindowsThemeMode.DarkThemePath, unmanagedPatched: dark);
            }

            

            if (builder.Config.WindowsThemeMode.MonitorActiveTheme)
            {
                WindowsThemeMonitor.ResumeThemeMonitor();
            }
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
                    themePath = Helper.PathUnmanagedLightTheme;
                    break;

                case Theme.Dark:
                    themePath = Helper.PathUnmanagedDarkTheme;
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

        public static string GetCurrentVisualStyleName()
        {
            return Path.GetFileName(new ThemeManagerClass().CurrentTheme.VisualStyle);
        }

        public static bool ThemeModeNeedsUpdate(Theme newTheme, bool skipCheck = false)
        {
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
                (!state.UnmanagedActiveThemePath.Equals(Helper.PathUnmanagedDarkTheme))))
            {
                return true;
            }
            else if (newTheme == Theme.Light && (skipCheck ||
                (!state.UnmanagedActiveThemePath.Equals(Helper.PathUnmanagedLightTheme))))
            {
                return true;
            }
            return false;
        }

        public static void RefreshDwmFull(bool managed, SwitchEventArgs e)
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_22H2)
            {
                try
                {
                    // prepare theme
                    ThemeFile dwmRefreshTheme = new(Helper.PathDwmRefreshTheme);
                    // if unmanaged we need to force-sync the theme
                    if (!managed)
                    {
                        state.ManagedThemeFile.SyncWithActiveTheme(false, false, true);
                        ThemeFile unmanagedTarget;
                        if (e.Theme == Theme.Dark)
                        {
                            unmanagedTarget = new(builder.Config.WindowsThemeMode.DarkThemePath);
                        }
                        else
                        {
                            unmanagedTarget = new(builder.Config.WindowsThemeMode.LightThemePath);
                        }
                        try
                        {
                            unmanagedTarget.Load();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "dwm management: could not load unmanaged target theme while refreshing dwm, forcing refresh:");
                        }
                        if (unmanagedTarget.VisualStyles.AutoColorization.Item1 == "1")
                        {
                            Logger.Info("dwm management: no refresh required because auto colorization is enabled in the target theme");
                            return;
                        }
                        else if (unmanagedTarget.VisualStyles.ColorizationColor.Item1 != state.ManagedThemeFile.VisualStyles.ColorizationColor.Item1)
                        {
                            Logger.Info("dwm management: no refresh required because target theme has different accent color");
                            return;
                        }
                        dwmRefreshTheme.SetContentAndParse(unmanagedTarget.ThemeFileContent);
                    }
                    else
                    {
                        dwmRefreshTheme.SetContentAndParse(state.ManagedThemeFile.ThemeFileContent);
                    }
                    dwmRefreshTheme.RefreshGuid();
                    dwmRefreshTheme.DisplayName = "DwmRefreshTheme";

                    // get current accent color
                    string currentColorization = RegistryHandler.GetAccentColor().Replace("#", "0X");
                    string lastColorizationDigitString = currentColorization[currentColorization.Length - 1].ToString();
                    int lastColorizationDigit = int.Parse(lastColorizationDigitString, System.Globalization.NumberStyles.HexNumber);

                    // modify last digit
                    if (lastColorizationDigit >= 9) lastColorizationDigit--;
                    else lastColorizationDigit++;
                    string newColorizationColor = currentColorization[..(currentColorization.Length - 1)] + lastColorizationDigit.ToString("X");

                    // update theme
                    dwmRefreshTheme.VisualStyles.ColorizationColor = (newColorizationColor, dwmRefreshTheme.VisualStyles.ColorizationColor.Item2);
                    dwmRefreshTheme.VisualStyles.AutoColorization = ("0", dwmRefreshTheme.VisualStyles.AutoColorization.Item2);
                    dwmRefreshTheme.Save();

                    List<ThemeApplyFlags> flagList = new() { ThemeApplyFlags.IgnoreBackground, ThemeApplyFlags.IgnoreCursor, ThemeApplyFlags.IgnoreDesktopIcons, ThemeApplyFlags.IgnoreSound, ThemeApplyFlags.IgnoreScreensaver };
                    Apply(dwmRefreshTheme.ThemeFilePath, true, null, flagList);

                    Logger.Info("dwm management: refresh performed by theme handler");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "dwm management: could not refresh dwm due to malformed colorization string: ");
                }
            }
            else
            {
                Logger.Trace("dwm management: no refresh required needed in this windows version");
            }
        }

        private static bool ApplyIThemeManager(string originalPath, bool suppressLogging = false, ThemeFile unmanaged = null)
        {
            string themeFilePath = unmanaged != null ? unmanaged.ThemeFilePath : originalPath;
            bool success = false;
            /*Exception applyEx = null;*/
            Thread thread = new(() =>
            {
                try
                {
                    new ThemeManagerClass().ApplyTheme(themeFilePath);
                    state.UnmanagedActiveThemePath = themeFilePath;
                    if (!suppressLogging)
                    {
                        if (unmanaged != null) Logger.Info($"applied theme \"{originalPath}\" via IThemeManager");
                        else Logger.Info($"applied theme \"{themeFilePath}\" via IThemeManager");
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not apply theme \"{themeFilePath}\"");
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
            return success;
        }

        /// <summary>
        /// Applies a theme attempting to use IThemeManager2 first, falls back to IThemeManager if that fails
        /// </summary>
        /// <param name="originalPath">the original path of the theme</param>
        /// <param name="suppressLogging"></param>
        /// <param name="unmanagedPatched">An optional unmanaged theme path. Will use this theme path to apply the theme instead of the original path. <br/>
        /// This is used such that a copied theme can be applied while still showing the correct origin path. Only set this if you have an unmanaged theme copy.</param>
        /// <param name="flagList"></param>
        private static void ApplyIThemeManager2(string originalPath, bool suppressLogging = false, ThemeFile unmanagedPatched = null, List<ThemeApplyFlags> flagList = null)
        {
            DateTime start = DateTime.Now;

            string themeFilePath = unmanagedPatched != null ? unmanagedPatched.ThemeFilePath : originalPath;

            bool tm2Found = false;
            bool tm2Success = false;
            bool tmSuccess = false;
            string displayNameFromFile = null;
            try
            {
                if (builder.Config.WindowsThemeMode.Enabled)
                {
                    if (flagList == null) flagList = builder.Config.WindowsThemeMode.ApplyFlags;
                    else builder.Config.WindowsThemeMode.ApplyFlags.ForEach(f => { if (!flagList.Contains(f)) flagList.Add(f); });
                }
                (_, _, displayNameFromFile) = ThemeFile.GetDisplayNameFromRaw(themeFilePath);
                (tm2Found, tm2Success) = IThemeManager2.Tm2Handler.SetTheme(displayNameFromFile, originalPath, flagList: flagList, suppressLogging: suppressLogging);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not retrieve display name for path {themeFilePath}:");
            }

            if (!tm2Found && tm2Success)
            {
                Logger.Warn("theme name not found in IThemeManager2, using mitigation (ignores ignore flags)");
                ApplyIThemeManager(themeFilePath, suppressLogging);
                string displayNameApi = GetCurrentThemeName();
                if (!state.LearnedThemeNames.ContainsKey(displayNameFromFile))
                {
                    Logger.Debug($"learned new theme name association: {displayNameFromFile}={displayNameApi}");
                    state.LearnedThemeNames.Add(displayNameFromFile, displayNameApi);
                }
                else
                {
                    Logger.Debug($"updated theme name association: {displayNameFromFile}={displayNameApi}");
                    state.LearnedThemeNames[displayNameFromFile] = displayNameApi;
                }
            }

            if (tm2Success || tmSuccess) state.UnmanagedActiveThemePath = themeFilePath;

            DateTime end = DateTime.Now;
            TimeSpan elapsed = end - start;

            if (elapsed.TotalSeconds > 10 && tm2Success)
            {
                Logger.Warn($"theme switching took longer than expected ({elapsed.TotalSeconds} seconds)");
            }
        }
    }
}