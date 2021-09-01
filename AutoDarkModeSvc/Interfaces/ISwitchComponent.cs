using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Interfaces
{
    interface ISwitchComponent
    {
        public void Init();
        public void Enabled();

        /// <summary>
        /// Switches to the desired theme
        /// </summary>
        /// <param name="newTheme"></param>
        public void Switch(Theme newTheme);
        /// <summary>
        /// Checks if the component needs to be updated, i.e Switch() needs to be called
        /// </summary>
        /// <returns></returns>
        public int PriorityToDark { get; }
        /// <summary>
        /// Priority for switching to light mode
        /// </summary>
        public int PriorityToLight { get; }
    }
}
