using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Communication
{
    class AsyncPipeServer : IMessageServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool Running { get; set; } = true;
        private CancellationTokenSource stoptokenSource;
        private Service Service { get; }

        public AsyncPipeServer(Service service)
        {
            Service = service;
        }
        public void Start()
        {
            stoptokenSource = new();
            Task.Run(async () =>
            {
                Logger.Info($"started message server (dual pipe), request: {Address.PipePrefix + Address.PipeRequest}, response: {Address.PipePrefix + Address.PipeResponse}");
                while (Running)
                {
                    string msg = await HandleRequest();
                    if (msg == null) continue;
                    HandleResponse(msg);
                }
            });
        }
        public void Stop()
        {
            Running = false;
            stoptokenSource.Cancel();
            Logger.Info("message server stopped");
        }

        private async Task<string> HandleRequest()
        {
            string msg;
            try
            {
                using NamedPipeServerStream requestPipe = new(Address.PipePrefix + Address.PipeRequest, PipeDirection.InOut, 5, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await requestPipe.WaitForConnectionAsync(stoptokenSource.Token);

                if (requestPipe.IsConnected && requestPipe.CanRead)
                {
                    using StreamReader sr = new(requestPipe);
                    msg = sr.ReadLine();
                    Logger.Debug("received message: {0}", msg);
                }
                else
                {
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Debug("cancellation token invoked during request");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in message server request:");
                return null;
            }
            return msg;
        }

        private async void HandleResponse(string msg)
        {
            try
            {
                CancellationTokenSource timeoutTokenSource = new();
                timeoutTokenSource.CancelAfter(5000);
                using NamedPipeServerStream responsePipe = new(Address.PipePrefix + Address.PipeResponse, PipeDirection.InOut, 5, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await responsePipe.WaitForConnectionAsync(timeoutTokenSource.Token);

                string response = "";
                MessageParser.Parse(new List<string>() { msg }, (message) =>
                {
                    response = message;
                }, Service);
                StreamWriter sw = new StreamWriter(responsePipe)
                { AutoFlush = true };
                using (sw)
                {
                    sw.Write(response);
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Warn("no client waiting for response, processing request anyway");
                MessageParser.Parse(new List<string>() { msg }, (message) => { }, Service);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in message server:");
            }
        }
    }
}
