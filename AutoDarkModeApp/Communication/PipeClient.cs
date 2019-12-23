using System;
using System.IO;
using System.IO.Pipes;

namespace AutoDarkModeApp.Communication
{
    class PipeClient
    {
        private string PipeName { get; set; }
        private int Count;

        public PipeClient(string pipename)
        {
            PipeName = pipename;
            Count = 0;
        }

        public void SendMessage(string message)
        {
            while (Count++ < 3)
            {
                System.Threading.Thread.Sleep(3000);
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect();
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.AutoFlush = true;
                            sw.WriteLine(message);
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("Could not write to pipe: {0}", e.Message);
                    }
                }
            }
           
        }
    }
}
