using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeSvc.Communication
{
    interface ICommandServer
    {
        public void Start();
        public void Stop();
    }
}
