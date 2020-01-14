using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Communication
{
    class PipeClient : ICommandClient
    {
        private string PipeName { get; set; }
        public PipeClient(string pipename)
        {
            PipeName = pipename;
        }

        /// <summary>
        /// Sends a message through the pipe
        /// </summary>
        /// <param name="message">Message string to be sent via the pipe</param>
        /// <returns>true if no error occurred; false otherwise</returns>
        public bool SendMessage(string message)
        {
            //this is needed. If ReceiveResponse is called in PipeMessenger for some reason the application will deadlock.
            PipeMessenger(message);
            return ReceiveReponse();
        }

        private void PipeMessenger(string message)
        {
            using NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName + Tools.DefaultPipeCommand, PipeDirection.Out);
            pipeClient.Connect(5000);
            using StreamWriter sw = new StreamWriter(pipeClient);
            sw.AutoFlush = true;
            sw.WriteLine(message);
        }

        private bool ReceiveReponse()
        {
            bool ok = true;
            using NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName + Tools.DefaultPipeResponse, PipeDirection.In);
            try
            {
                pipeClient.Connect(1000);
                using StreamReader sr = new StreamReader(pipeClient);
                string temp;
                while ((temp = sr.ReadLine()) != null)
                {
                    if (temp.Contains(Tools.Err))
                    {
                        ok = false;
                    }
                }
            }
            catch (TimeoutException)
            {
                return false;
            }
            return ok;
        }
    }
}
