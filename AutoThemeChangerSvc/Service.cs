using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoThemeChangerSvc.Communication;

namespace AutoThemeChangerSvc
{
    class Service
    {
        static void Main(string[] args)
        {
            CancellationTokenSource StopTokenSource = new CancellationTokenSource();
            CancellationToken ct = StopTokenSource.Token;
            PipeServer ps = new PipeServer("WindowsAutoDarkMode");

            Task.Run(() =>
            {
                ps.StartServer();
            }, ct);                       
        }
    }
}
