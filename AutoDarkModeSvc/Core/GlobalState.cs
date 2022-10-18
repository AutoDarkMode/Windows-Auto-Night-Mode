using System;
using System.Collections.Generic;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeSvc.Modules;

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
        protected GlobalState() {
             PostponeManager = new(this);
        }

        private WardenModule Warden { get; set; }
        /// <summary>
        /// The theme that was last requested. This either reflects the current internal theme of ADM, 
        /// or the theme shortly before a switch will be performed
        /// </summary>
        public Theme RequestedTheme { get; set; } = Theme.Unknown;
        public Theme CurrentWallpaperTheme { get; set; } = Theme.Unknown;
        public Theme ForcedTheme { get; set; } = Theme.Unknown;
        public string UnmanagedActiveThemePath { get; set; } = "";
        public ThemeFile ManagedThemeFile { get; } = new(Helper.ManagedThemePath);
        public PostponeManager PostponeManager { get; }
        public NightLight NightLight { get; } = new();
        public bool InitSyncSwitchPerformed { get; set; } = false;
        private NotifyIcon NotifyIcon { get; set; }
        public Dictionary<string, string> LearnedThemeNames { get; } = new();

        public void InitThemes(AdmConfig config)
        {
            Logger.Debug("retrieving theme on startup");
            if (config.WindowsThemeMode.Enabled)
            {
                try
                {
                    UnmanagedActiveThemePath = RegistryHandler.GetActiveThemePath();

                    bool unmanagedLight = UnmanagedActiveThemePath.Equals(Helper.UnmanagedLightThemePath);
                    bool unmanagedDark = UnmanagedActiveThemePath.Equals(Helper.UnmanagedDarkThemePath);
                    if (unmanagedLight)
                    {
                        string displayNameUnmanaged = ThemeFile.GetOriginalNameFromRaw(Helper.UnmanagedLightThemePath);
                        (_, string displayNameSource) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.LightThemePath);
                        if (displayNameUnmanaged != displayNameSource) UnmanagedActiveThemePath = "";
                    }
                    if (unmanagedDark)
                    {
                        string displayNameUnmanaged = ThemeFile.GetOriginalNameFromRaw(Helper.UnmanagedDarkThemePath);
                        (_, string displayNameSource) = ThemeFile.GetDisplayNameFromRaw(config.WindowsThemeMode.DarkThemePath);
                        if (displayNameUnmanaged != displayNameSource) UnmanagedActiveThemePath = "";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not retrieve active theme path: ");
                }
            }
            else
            {
                ManagedThemeFile.SyncWithActiveTheme();
            }
        }

        public EventWaitHandle ConfigIsUpdatingWaitHandle { get; } = new ManualResetEvent(true);

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

            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                if (PostponeManager.IsPostponed || PostponeManager.IsUserDelayed) NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconPausedTray;
                else NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconTray;
            }
            else NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconDisabledTray;
        }
    }

    public class NightLight
    {
        public Theme Current { get; set; } = Theme.Unknown;
    }
}
