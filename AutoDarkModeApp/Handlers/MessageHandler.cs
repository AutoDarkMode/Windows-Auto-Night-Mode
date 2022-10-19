using AutoDarkModeComms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Handlers
{
    class MessageHandler
    {
        public static IMessageClient Client { get; } = new PipeClient();
    }
}
