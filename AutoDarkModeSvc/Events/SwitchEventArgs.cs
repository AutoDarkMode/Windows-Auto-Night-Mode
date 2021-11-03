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
        public SwitchSource Source { get; }
    }
}