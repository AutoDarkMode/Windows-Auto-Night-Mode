using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Communication
{
    class ZeroMQServer : ICommandServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string Port { get; }
        private Service Service { get; }
        private ResponseSocket Server { set; get; }
        private NetMQPoller Poller { get; set; }
        private Task PollTask { get; set; }

        public ZeroMQServer(string port, Service service)
        {
            Port = port;
            Service = service;
        }

        private bool AcceptConnections { get; set; }

        public void Start()
        {
            Server = new ResponseSocket("tcp://127.0.0.1:" + Port);
            Poller = new NetMQPoller { Server };
            Server.ReceiveReady += (s, a) =>
            {
                string msg = a.Socket.ReceiveFrameString();
                Logger.Info("received message: {0}", msg);
                MessageParser.Parse(new List<string>() { msg }, (message) =>
                {
                    try
                    {
                        a.Socket.TrySendFrame(new TimeSpan(10000000), message);
                    } catch (Exception e)
                    {
                        Logger.Error(e, "could not send response");
                    }
                }, Service);
            };
            PollTask = Task.Run(() =>
            {
                Poller.Run();
            });
            Logger.Info("started server (polling)");
        }

        public void Stop()
        {
            Logger.Info("stopping server");
            Poller.Stop();
            PollTask.Wait();
            try
            {
                Poller.Dispose();
            } catch (Exception ex)
            {
                Logger.Fatal(ex, "could not dispose Poller");
            }
            Server.Dispose();
            NetMQConfig.Cleanup();
        }
    }
}
