using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeComms
{
    public class ZeroMQClient : ICommandClient
    {
        private string Port { get;  }
        public ZeroMQClient(string port)
        {
            Port = port;
        }

        private string GetBackendPort()
        {
            try
            {
                using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("adm-backend-port");
                using MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor();
                byte[] bytes = new byte[sizeof(int)];
                viewAccessor.ReadArray(0, bytes, 0, bytes.Length);
                int backendPort = BitConverter.ToInt32(bytes, 0);
                return backendPort.ToString();
            }
            catch
            {
                return Port;
            }
        }

        public bool SendMessage(string message, int timeoutSeconds)
        {

            using (var client = new RequestSocket())
            {
                client.Connect("tcp://127.0.0.1:" + GetBackendPort());
                client.SendFrame(message);
                var response = GetResponse(client, timeoutSeconds);
                if (response.Contains(StatusCode.Err))
                {
                    return false;
                }
                else if (response.Contains(StatusCode.Ok))
                {
                    return true;
                }
            }
            return false;
        }

        public string SendMessageWithRetries(string message, int timeoutSeconds, int retries)
        {
            for (int i = 0; i < retries; i++)
            {
                using RequestSocket client = new RequestSocket();
                client.Connect("tcp://127.0.0.1:" + GetBackendPort());
                client.SendFrame(message);
                string response = GetResponse(client, timeoutSeconds);
                if (response != StatusCode.Timeout)
                {
                    return response;
                }
            }
            return StatusCode.Timeout;
        }

        private string GetResponse(RequestSocket client, int timeoutSeconds)
        {
            bool hasResponse = false;
            string response;
            hasResponse = client.TryReceiveFrameString(new TimeSpan(0, 0, timeoutSeconds), out response);

            if (hasResponse)
            {
                return response;   
            }
            return StatusCode.Timeout;
        }

        public string SendMessageAndGetReply(string message, int timeoutSeconds)
        {
            using var client = new RequestSocket();
            client.Connect("tcp://127.0.0.1:" + GetBackendPort());
            client.SendFrame(message);
            return GetResponse(client, timeoutSeconds);
        }

        public Task<bool> SendMessageAsync(string message, int timeoutSeconds)
        {
            return Task.Run(() => SendMessage(message, timeoutSeconds));
        }

        public Task<string> SendMessageAndGetReplyAsync(string message, int timeoutSeconds)
        {
            return Task.Run(() => SendMessageAndGetReply(message, timeoutSeconds));
        }
    }
}
