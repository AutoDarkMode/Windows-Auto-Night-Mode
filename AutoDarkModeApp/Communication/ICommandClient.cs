using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeApp.Communication
{
    interface ICommandClient
    {
        public bool SendMessage(string message);
    }
}
