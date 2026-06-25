using AutoDarkModeLib;

namespace AutoDarkModeSvc.Events;

public class DwmRefreshEventArgs(DwmRefreshSource refreshSource, int delay = 0, DwmRefreshType type = DwmRefreshType.Standard)
{
    public DwmRefreshSource RefreshSource => refreshSource;
    public DwmRefreshType Type => type;
    public int Delay => delay;
}
