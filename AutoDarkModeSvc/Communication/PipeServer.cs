using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using AutoDarkModeApp;

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
                        List<string> msg = new List<string>();
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            msg.Add(temp);
                        }
                        MsgParser(msg);
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

        public void MsgParser(List<string> msg)
        {
            Handler.RegEdit rh = new Handler.RegEdit();
            Handler.TaskSchd tschd = new Handler.TaskSchd();

            msg.ForEach(message =>
            {
                switch (message)
                {
                    case Tools.Switch: 
                        rh.SwitchThemeBasedOnTime();
                        break;
                    case Tools.Swap:
                        if (rh.AppsUseLightTheme())
                        {
                            rh.ThemeToDark();
                        }
                        else
                        {
                            rh.ThemeToLight();
                        }
                        break;
                    case Tools.Dark:
                        rh.ThemeToDark();
                        break;
                    case Tools.Light:
                        rh.ThemeToLight();
                        break;
                    case Tools.AddAutostart:
                        rh.AddAutoStart();
                        break;
                    case Tools.RemoveAutostart:
                        rh.RemoveAutoStart();
                        break;
                    case Tools.CreateTask:
                        Console.WriteLine("Not implemented yet");
                        break;
                    case Tools.RemoveTask:
                        tschd.RemoveTask();
                        break;
                }
            });
        }
    }
}