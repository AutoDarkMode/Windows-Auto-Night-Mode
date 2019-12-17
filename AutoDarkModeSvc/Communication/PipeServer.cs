using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;

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
            Console.WriteLine("Successfully stopped listen loop");
        }

        public void MsgParser(List<string> msg)
        {
            AutoDarkModeConfigBuilder Properties = AutoDarkModeConfigBuilder.GetInstance();
            Handler.RegistryHandler rh = new Handler.RegistryHandler();
            Handler.TaskSchdHandler tschd = new Handler.TaskSchdHandler();

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
                        try
                        {
                            DateTime sunrise = Convert.ToDateTime(Properties.Config.Time.SunRise);
                            DateTime sunset = Convert.ToDateTime(Properties.Config.Time.SunSet);
                            tschd.CreateTask(sunrise.Hour, sunrise.Minute, sunset.Hour, sunset.Minute);
                        }
                        catch (FormatException e)
                        {
                            //todo: logger here!
                            Console.WriteLine(e);
                        }

                        break;
                    case Tools.RemoveTask:
                        tschd.RemoveTask();
                        break;
                }
            });
        }
    }
}