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
        private ResponseSocket Server { set; get; }
        private NetMQPoller Poller { get; set; }
        private Task PollTask { get; set; }

        public ZeroMQServer(string port)
        {
            Port = port;
        }

        private bool AcceptConnections { get; set; }

        public void Start()
        {
            Server = new ResponseSocket("tcp://127.0.0.1:" + Port);
            Poller = new NetMQPoller { Server };
            Server.ReceiveReady += (s, a) =>
            {
                string msg = a.Socket.ReceiveFrameString();
                Logger.Debug("received message via ZeroMQ: {0}", msg);
                MessageParser.Parse(new List<string>() { msg }, (message) =>
                {
                    try
                    {
                        a.Socket.TrySendFrame(new TimeSpan(1000000), message);
                    } catch (Exception e)
                    {
                        Logger.Error(e, "could not send ZeroMQ response");
                    }
                });
            };
            PollTask = Task.Run(() =>
            {
                Poller.Run();
            });
            Logger.Debug("started ZeroMQ server with polling");
        }

        public void Stop()
        {
            Logger.Debug("stopping ZeroMQ Server");
            Poller.Stop();
            PollTask.Wait();
            try
            {
                Poller.Dispose();
            } catch (Exception ex)
            {
                Logger.Fatal(ex, "could not dispose ZeroMQ Poller");
            }
            Server.Dispose();
        }
    }
}
