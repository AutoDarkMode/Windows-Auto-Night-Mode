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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Modules;
using AdmProperties = AutoDarkModeLib.Properties;


namespace AutoDarkModeSvc.Core
{
    public class GlobalState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static GlobalState state;
        public static GlobalState Instance()
        {
            if (state == null)
            {
                state = new GlobalState();
            }
            return state;
        }
        protected GlobalState()
        {
            PostponeManager = new(this);
        }

        private WardenModule Warden { get; set; }

        public Theme _requestedTheme = Theme.Unknown;
        /// <summary>
        /// The theme that was last requested to be set. This either reflects the already applied theme, 
        /// or the pending theme shortly before a switch will be performed
        /// </summary>
        public Theme InternalTheme
        {
            get { return _requestedTheme; }
            set
            {
                _requestedTheme = value;
                try
                {
                    UpdateNotifyIcon(AdmConfigBuilder.Instance());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "failed updating tray icon theme status:");
                }
            }
        }
        public Theme ForcedTheme { get; set; } = Theme.Unknown;
        public string UnmanagedActiveThemePath { get; set; } = "";
        public ThemeFile ManagedThemeFile { get; } = new(Helper.PathManagedTheme);
        public PostponeManager PostponeManager { get; }
        public NightLight NightLight { get; } = new();
        public SystemIdleModuleState SystemIdleModuleState { get; } = new();
        public bool InitSyncSwitchPerformed { get; set; } = false;
        private NotifyIcon NotifyIcon { get; set; }
        public Dictionary<string, string> LearnedThemeNames { get; } = new();
        public EventWaitHandle ConfigIsUpdatingWaitHandle { get; } = new ManualResetEvent(true);
        public SwitchApproach SwitchApproach { get; } = new();
        private bool configIsUpdating;
        public bool ConfigIsUpdating
        {
            get { return configIsUpdating; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set { configIsUpdating = value; }
        }

        private bool geolocatorIsUpdating;
        public bool GeolocatorIsUpdating
        {
            get { return geolocatorIsUpdating; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set { geolocatorIsUpdating = value; }
        }

        /// <summary>
        /// Setting this value to true will skip the next config reload event when it has been saved
        /// The setting will return to false after the first save
        /// </summary>
        public bool SkipConfigFileReload { get; set; }
        public string CurrentWallpaperPath { get; set; }

        /// <summary>
        /// This method is responsible for updating the internal UnmanagedActiveThemePath variable. <br/>
        /// Without this ADM would have no idea if the applied theme is correct. <br/>
        /// We append an UnmanagedOriginalName entry to each of adm's light or dark unmanaged theme
        /// file. <br/> That tells us on what theme file it was based on. This way we can prevent unnecessary theme switches <br/>
        /// Appending this extra parameter is done in ThemeHandler.ApplyTheme
        /// </summary>
        /// <param name="config">current adm config object</param>
        public void RefreshThemes(AdmConfig config)
        {
            if (config.WindowsThemeMode.Enabled)
            {
                try
                {
                    UnmanagedActiveThemePath = RegistryHandler.GetActiveThemePath();

                    // for unmanaged with flags we need to set the unmanagedactivethemepath to our internal names
                    // This is because when using apply flags, the theme is reported as custom. However, our UnmanagedOriginalName
                    // persists because we have set it originally when applying the theme, so if we read that then we are aware of the origin theme
                    if (config.WindowsThemeMode.ApplyFlags != null && config.WindowsThemeMode.ApplyFlags.Count > 0)
                    {
                        string customPath = Path.Combine(Helper.PathThemeFolder, "Custom.theme");
                        bool unmanagedCustom = UnmanagedActiveThemePath.Equals(customPath);
                        if (unmanagedCustom)
                        {
                            UnmanagedActiveThemePath = "";
                            // retrieve theme names from configuration file and then compare them to the custom path
                            (_, _, string displayNameLight) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.LightThemePath);
                            (_, _, string displayNameDark) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.DarkThemePath);
                            string sourceThemeNameCustom = ThemeFile.GetOriginalNameFromRaw(customPath);
                            if (sourceThemeNameCustom == displayNameDark)
                            {
                                UnmanagedActiveThemePath = Helper.PathUnmanagedDarkTheme;
                            }
                            else if (sourceThemeNameCustom == displayNameLight)
                            {
                                UnmanagedActiveThemePath = Helper.PathUnmanagedLightTheme;
                            }
                            Logger.Debug($"refresh theme state with apply flags enabled, active theme: {(UnmanagedActiveThemePath == "" ? "undefined" : UnmanagedActiveThemePath)}");
                        }
                    }
                    // for unmanaged without applyflags
                    else
                    {
                        bool unmanagedLight = UnmanagedActiveThemePath.Equals(Helper.PathUnmanagedLightTheme);
                        bool unmanagedDark = UnmanagedActiveThemePath.Equals(Helper.PathUnmanagedDarkTheme);

                        // check if an unmanaged theme is active. If so, extract the original name from the theme path
                        // This way, we know which origin theme is active and don't have to switch
                        if (unmanagedLight)
                        {
                            string displayNameUnmanaged = ThemeFile.GetOriginalNameFromRaw(Helper.PathUnmanagedLightTheme);
                            (_, _, string displayNameSource) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.LightThemePath);
                            if (displayNameUnmanaged != displayNameSource)
                            {
                                Logger.Debug($"detected change in unmanaged light theme, new origin: {config.WindowsThemeMode.LightThemePath}");
                                UnmanagedActiveThemePath = "";
                            }
                        }
                        if (unmanagedDark)
                        {
                            string displayNameUnmanaged = ThemeFile.GetOriginalNameFromRaw(Helper.PathUnmanagedDarkTheme);
                            (_, _, string displayNameSource) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.DarkThemePath);
                            if (displayNameUnmanaged != displayNameSource)
                            {
                                Logger.Debug($"detected change in unmanaged light theme, new origin: {config.WindowsThemeMode.DarkThemePath}");
                                UnmanagedActiveThemePath = "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not retrieve active theme path: ");
                }
            }
            else
            {
                // we're only reading here, so we can't apply the theme fix safely
                ManagedThemeFile.SyncWithActiveTheme(false);
            }
        }

        public void SetWarden(WardenModule warden)
        {
            Warden = warden;
        }

        public void SetNotifyIcon(NotifyIcon icon)
        {
            NotifyIcon ??= icon;
        }

        public void UpdateNotifyIcon(AdmConfigBuilder builder)
        {
            if (NotifyIcon == null) return;

            string themeState = "";
            if (InternalTheme != Theme.Unknown)
            {
                if (InternalTheme == Theme.Light)
                {
                    themeState = AdmProperties.Resources.lblLight;
                }
                else
                {
                    themeState = AdmProperties.Resources.lblDark;
                }
            }

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (PostponeManager.IsUserDelayed || PostponeManager.IsSkipNextSwitch || PostponeManager.IsGracePeriod)
                {
                    NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconPausedTray;
                    NotifyIcon.Text = $"Auto Dark Mode\n{themeState} - {AdmProperties.Resources.lblPaused}";
                }
                else
                {
                    NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconTray;
                    NotifyIcon.Text = $"Auto Dark Mode\n{themeState} - {AdmProperties.Resources.enabled}";
                }
            }
            else
            {
                NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconDisabledTray;
                if (themeState.Length > 0) NotifyIcon.Text = $"Auto Dark Mode\n{themeState} - {AdmProperties.Resources.disabled}";
                else NotifyIcon.Text = $"Auto Dark Mode\nDisabled";
            }
        }
    }

    public class NightLight
    {
        /// <summary>
        /// The theme that Windows night light is currently requesting to bet set
        /// </summary>
        public Theme Requested { get; set; } = Theme.Unknown;
    }

    public class SystemIdleModuleState
    {
        public bool SystemIsIdle { get; set; } = false;
    }

    public class SwitchApproach
    {
        public bool ThemeSwitchApproaching { get; set; }
        private List<IAutoDarkModeModule> Dependencies { get; set; } = new();
        public bool DependenciesPresent { get { return Dependencies.Count > 0; } }
        public void AddDependency(IAutoDarkModeModule module)
        {
            if (!Dependencies.Contains(module))
            {
                Dependencies.Add(module);
            }
        }
        public void RemoveDependency(IAutoDarkModeModule module)
        {
            Dependencies.Remove(module);
        }

        public async Task TriggerDependencyModules()
        {
            foreach (var module in Dependencies)
            {
                await module.Fire();
            }
        }

    }
}
