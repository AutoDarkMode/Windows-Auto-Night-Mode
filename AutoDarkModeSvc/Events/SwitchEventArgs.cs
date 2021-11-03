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

    public enum SwitchSource
    {
        Any,
        TimeSwitchModule,
        BatteryStatus,
        SystemResume,
        Manual,
    }
}

