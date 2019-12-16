using System;
using System.IO;
using System.IO.Pipes;

namespace AutoDarkModeSvc.Communication
{
    class PipeServer
    {
        private string PipeName { get; set; }
        public bool Running { get; private set; }
        private bool AcceptConnections;


        public PipeServer(string pipename)
        {
            PipeName = pipename;
            Running = false;
            AcceptConnections = false;
        }

        public void StartServer()
        {
            Running = true;
            AcceptConnections = true;
            while (AcceptConnections)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
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

            Running = false;
        }

        public void StopServer()
        {
            AcceptConnections = false;
            while (Running)
            {
                using (NamedPipeClientStream npcs = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {

                    try
                    {
                        npcs.Connect(100);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Waiting for pipe to disconnect...");
                    }
                }
            }
            Console.WriteLine("Successfully stopped PipeServer");
        }
    }
}