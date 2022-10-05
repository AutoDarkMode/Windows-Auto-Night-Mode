using AutoDarkModeConfig;
using System;

namespace AutoDarkModeSvc.Events
{
    public class SwitchEventArgs : EventArgs
    {
        public SwitchEventArgs(SwitchSource source)
        {
            Source = source;
        }

        public SwitchEventArgs(SwitchSource source, Theme requestedTheme)
        {
            Source = source;
            RequestedTheme = requestedTheme;
        }
        public SwitchSource Source { get; }
        public Theme? RequestedTheme { get; } = null;
    }
}