using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Communication
{
    interface IMessageServer
    {
        /// <summary>
        /// Start a command server that receives command messages
        /// </summary>
        public void Start();
        /// <summary>
        /// stop a command server and release all used resources
        /// </summary>
        public void Stop();
    }
}
