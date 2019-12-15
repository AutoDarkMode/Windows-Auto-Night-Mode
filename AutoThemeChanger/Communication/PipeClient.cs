using System;
using System.IO;
using System.IO.Pipes;

namespace AutoThemeChanger.Communication
{
    class PipeClient
    {
        private string PipeName { get; set; }

        public PipeClient(string pipename)
        {
            PipeName = pipename;
        }

        public bool SendMessage(string message)
        {
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
                    return false;
                }
                return true;
            }
        }
    }
}
