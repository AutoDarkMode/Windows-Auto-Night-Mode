using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace AutoThemeChangerSvc.Communication
{
    class PipeServer
    {
        private string PipeName { get; set; }


        public PipeServer(string pipename)
        {
            PipeName = pipename;
        }

        public void StartServer()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut))
            {
                while (true)
                {
                    pipeServer.WaitForConnection();
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            Console.WriteLine("Message from Pipe: {0}", temp);
                        }
                    }
                }
            }
        }
    }
}
