using AutoDarkModeLib;
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
            Theme = requestedTheme;
        }

        public SwitchEventArgs(SwitchSource source, Theme requestedTheme, DateTime time)
        {
            Source = source;
            Theme = requestedTheme;
            Time = time;
        }

        public SwitchSource Source { get; }
        public Theme? Theme { get; } = null;
        public DateTime? Time { get; } = null;
    }
}