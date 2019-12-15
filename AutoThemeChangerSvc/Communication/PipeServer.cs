using System;
using System.IO;
using System.IO.Pipes;

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
