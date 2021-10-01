using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;

namespace AutoDarkModeSvc.Config
{
    class GlobalState
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static GlobalState rtc;
        public static GlobalState Instance()
        {
            if (rtc == null)
            {
                rtc = new GlobalState();
            }
            return rtc;
        }
        protected GlobalState()
        {
            CurrentWallpaperTheme = Theme.Unknown;
            CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
            ForcedTheme = Theme.Unknown;
            PostponeSwitch = false;
        }

        private WardenModule Warden { get; set; }
        public Theme CurrentWallpaperTheme { get; set; }
        public Theme ForcedTheme { get; set; }
        public string CurrentWindowsThemeName { get; set; }
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
    }
}
