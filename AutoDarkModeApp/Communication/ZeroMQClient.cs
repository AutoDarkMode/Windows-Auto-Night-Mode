using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Generic;
using System.Text;
using AutoDarkMode;
using System.Threading.Tasks;

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
                var response = GetResponse(client, message);
                if (response.Contains(Command.Err))
                {
                    return false;
                }
                else if (response.Contains(Command.Ok))
                {
                    return true;
                }
            }
            return false;
        }

        private string GetResponse(RequestSocket client, string message)
        {
            bool hasResponse = false;
            string response;
            lock (this)
            {
                hasResponse = client.TryReceiveFrameString(new TimeSpan(0, 0, 3), out response);
            }

            if (hasResponse)
            {
                return response;   
            }
            return Command.Err;
        }

        public string SendMessageAndGetReply(string message)
        {
            using var client = new RequestSocket();
            client.Connect("tcp://127.0.0.1:" + Port);
            client.SendFrame(message);
            return GetResponse(client, message);
        }

        public Task<bool> SendMessageAsync(string message)
        {
            return Task.Run(() => SendMessage(message));
        }

        public Task<string> SendMesssageAndGetReplyAsync(string message)
        {
            return Task.Run(() => SendMessageAndGetReply(message));
        }
    }
}
