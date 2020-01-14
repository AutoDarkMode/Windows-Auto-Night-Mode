using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeApp.Communication
{
    class ZeroMQClient : ICommandClient
    {
        private string Port { get;  }
        public ZeroMQClient(string port)
        {
            Port = port;
        }

        public bool SendMessage(string message)
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://127.0.0.1:" + Port);
                client.SendFrame(message);
                var hasResponse = client.TryReceiveFrameString(new TimeSpan(10000000), out string response);
                if (hasResponse)
                {
                    if (response.Contains(Tools.Err))
                    {
                        return false;
                    }
                    else if (response.Contains(Tools.Ok))
                    {
                        return true;
                    }
                }                
                return true;
            }
        }
    }
}
