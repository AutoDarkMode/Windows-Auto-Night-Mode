using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeComms
{
    public class PipeClient : IMessageClient
    {
        public string SendMessageAndGetReply(string message, int timeoutSeconds = 5)
        {
            using NamedPipeClientStream clientPipeRequest = new NamedPipeClientStream(".", Address.PipePrefix + Address.PipeRequest, PipeDirection.Out);
            try
            {
                clientPipeRequest.Connect(timeoutSeconds * 1000);
                StreamWriter sw = new StreamWriter(clientPipeRequest) { AutoFlush = true };
                using (sw)
                {
                    sw.Write(message);
                }
            }
            catch (Exception)
            {
                return new ApiResponse()
                {
                    StatusCode = StatusCode.Timeout,
                    Message = "The service did not acknowledge the req in time"
                }.ToString();
            }

            using NamedPipeClientStream clientPipeResponse = new NamedPipeClientStream(".", Address.PipePrefix + Address.PipeResponse, PipeDirection.In);
            try
            {
                clientPipeResponse.Connect(timeoutSeconds * 1000);
                if (clientPipeResponse.IsConnected && clientPipeResponse.CanRead)
                {
                    using StreamReader sr = new(clientPipeResponse);
                    //sr.BaseStream.ReadTimeout = timeoutSeconds * 1000;
                    string msg = sr.ReadToEnd();
                    if (msg == null)
                    {
                        return StatusCode.Timeout;
                    }
                    return msg;
                }
                else
                {
                    return new ApiResponse()
                    {
                        StatusCode = StatusCode.Err,
                        Message = "Pipe not connected or can't read"
                    }.ToString();
                }
            }
            catch (Exception)
            {
                return new ApiResponse()
                {
                    StatusCode = StatusCode.Timeout,
                    Message = "The service did not respond in time"
                }.ToString();
            }
        }

        public Task<string> SendMessageAndGetReplyAsync(string message, int timeoutSeconds = 5)
        {
            return Task.Run(() => SendMessageAndGetReply(message, timeoutSeconds));
        }

        public string SendMessageWithRetries(string message, int timeoutSeconds = 3, int retries = 3)
        {
            return SendMessageAndGetReply(message, timeoutSeconds * retries);
        }
    }
}
