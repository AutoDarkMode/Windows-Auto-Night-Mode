using System;

namespace AutoDarkModeLib.Configs;

public class PostponeData
{
    public Theme InternalThemeAtExit { get; set; } = Theme.Unknown;
    public DateTime LastModified { get; set; }
    public PostponeQueueDto Queue { get; set; } = new();
}
