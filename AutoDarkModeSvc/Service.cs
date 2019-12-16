using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeSvc
{
    class Service
    {
        public PipeServer ps;
        private Task task { get; set; }
        public Service()
        {
            ps = new PipeServer("WindowsAutoDarkMode");
        }

        public void Start()
        {
            task = Task.Run(() =>
            {
                ps.StartServer();
            });
            BackgroundTask ts = new BackgroundTask(this);
        }

        public void Stop()
        {
            ps.StopServer();
        }
    }
}
