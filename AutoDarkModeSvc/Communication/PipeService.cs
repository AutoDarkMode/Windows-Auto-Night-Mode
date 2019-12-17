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
            Console.WriteLine("Waiting for PipeServer to shut down");
            Task.Wait();
            Console.WriteLine("PipeServer thread shutdown confirmed");
        }
    }
}
