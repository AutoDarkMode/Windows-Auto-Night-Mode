using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Events
{
    class ExitEventArgs : EventArgs
    {
        public ExitEventArgs(bool closeApp)
        {
            CloseApp = closeApp;
        }

        public bool CloseApp { get; }
    }
}
