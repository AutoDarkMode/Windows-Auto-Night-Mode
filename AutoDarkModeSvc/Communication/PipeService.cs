using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeSvc.Communication
{
    class PipeService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public PipeServer ps;
        private Task Task { get; set; }
        public PipeService()
        {
            ps = new PipeServer("WindowsAutoDarkMode");
        }

        public void Start()
        {
            Task = Task.Run(() =>
            {
                ps.StartServer();
            });
        }

        public void Stop()
        {
            ps.StopServer();
            Logger.Info("Waiting for the pipe service thread to shut down");
            Task.Wait();
            Logger.Info("Pipe service thread shutdown confirmed");
        }
    }
}
