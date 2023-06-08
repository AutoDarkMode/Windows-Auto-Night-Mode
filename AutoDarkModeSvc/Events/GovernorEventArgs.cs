using AutoDarkModeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Events
{
    public class GovernorEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new Governor event. Used to inform the governor module what state a governor is in.
        /// </summary>
        /// <param name="inSwitchWindow">If the governor is currently within the defined switch window</param>
        /// <param name="switchEventArgs">The event args of the switch request</param>
        public GovernorEventArgs(bool inSwitchWindow, SwitchEventArgs switchEventArgs)
        {
            InSwitchWindow = inSwitchWindow;
            SwitchEventArgs = switchEventArgs;
        }

        /// <summary>
        /// Creates a new Governor event. Used to inform the governor module what state a governor is in.
        /// </summary>
        /// <param name="inSwitchWindow">If the governor is currently within the defined switch window</param>
        public GovernorEventArgs(bool inSwitchWindow)
        {
            InSwitchWindow = inSwitchWindow;
        }

        public bool InSwitchWindow { get; }
        public SwitchEventArgs SwitchEventArgs { get; } = null;
    }
}
