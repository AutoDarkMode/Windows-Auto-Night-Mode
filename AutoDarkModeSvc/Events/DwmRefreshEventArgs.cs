using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeLib;

namespace AutoDarkModeSvc.Events;
public class DwmRefreshEventArgs(DwmRefreshSource refreshSource, int delay = 0)
{
    public DwmRefreshSource RefreshSource => refreshSource;
    public int Delay => delay;
}
