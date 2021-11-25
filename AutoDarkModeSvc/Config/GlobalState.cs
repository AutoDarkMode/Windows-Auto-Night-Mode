using System;
using System.Collections.Generic;
using System.Management;
using System.Text;
using System.Threading;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;

namespace AutoDarkModeSvc.Config
{
    public class GlobalState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ManagementEventWatcher globalThemeEventWatcher;

        private static GlobalState state;
        public static GlobalState Instance()
        {
            if (state == null)
            {
                state = new GlobalState();
            }
            return state;
        }
        protected GlobalState() { }

        private WardenModule Warden { get; set; }
        public Theme LastRequestedTheme { get; set; } = Theme.Unknown;
        public Theme CurrentWallpaperTheme { get; set; } = Theme.Unknown;
        public Theme ForcedTheme { get; set; } = Theme.Unknown;
        public string CurrentWindowsThemeName { get; set; } = GetCurrentThemeName();
        private bool _postponeSwitch;
        // triggers update if and only if there is a change in value
        public bool PostponeSwitch
        {
            get { return _postponeSwitch; }
            set
            {
                if (value != _postponeSwitch)
                {
                    _postponeSwitch = value;
                    if (Warden != null)
                    {
                        Warden.Fire();
                    }
                }
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

        private static string GetCurrentThemeName()
        {
            string currentTheme = ThemeHandler.GetCurrentThemeName();
            Logger.Debug($"active windows theme on startup: {currentTheme}");
            return currentTheme;
        }
        private void HandleThemeMonitorEvent()
        {
            Logger.Debug("theme switch detected");
            Thread thread = new(() =>
            {
                try
                {
                    CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not update theme name");
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
                Logger.Error(ex, "error while waiting for thread to stop:");
            }
            ThemeManager.RequestSwitch(AdmConfigBuilder.Instance(), new(SwitchSource.ExternalThemeSwitch));
        }

        public void StartThemeMonitor()
        {
            try
            {
                if (globalThemeEventWatcher != null)
                {
                    return;
                }
                globalThemeEventWatcher = WMIHandler.CreateHKCURegistryValueMonitor(HandleThemeMonitorEvent, "SOFTWARE\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Themes", "CurrentTheme");
                globalThemeEventWatcher.Start();
                Logger.Info("theme monitor enabled");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not start active theme monitor");
            }
        }

        public void StopThemeMonitor()
        {
            try
            {
                if (globalThemeEventWatcher != null)
                {
                    globalThemeEventWatcher.Stop();
                    globalThemeEventWatcher.Dispose();
                    globalThemeEventWatcher = null;
                    Logger.Info("theme monitor disabled");
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not stop active theme monitor");
            }

        }
    }
}
