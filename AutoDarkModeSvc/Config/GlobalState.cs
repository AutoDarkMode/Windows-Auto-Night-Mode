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
            CurrentWallpaperTheme = Theme.Undefined;
            CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
            ForcedTheme = Theme.Undefined;
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
        public string CurrentWallpaperPath { get; set; }

        public void SetWarden(WardenModule warden)
        {
            Warden = warden;
        }
    }
}
