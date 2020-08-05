using AutoDarkModeApp.Communication;
using AutoDarkModeSvc.Config;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using AutoDarkModeSvc;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        private readonly AdmConfigBuilder autoDarkModeConfigBuilder = AdmConfigBuilder.Instance();
        private readonly ICommandClient commandClient = new ZeroMQClient(Command.DefaultPort);
        public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
        private bool debug = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Shutdown();
            }

            List<string> args = new List<string>();
            if (e.Args.Length > 0)
            {
                args = new List<string>(e.Args);
            }
            if (args.Count > 0 && args.Contains("/debug"))
            {
                debug = true;
                args.Remove("/debug");
            }

            StartService();
            int maxTries = 5;
            int tries = 0;
            bool heartBeatOK = false;
            while (tries < maxTries && !heartBeatOK)
            {
                tries++;
                heartBeatOK = commandClient.SendMessage(Command.Alive);
            }
            if (maxTries == tries && !heartBeatOK)
            {
                string error = "could not get a heartbeat from the backend. Check if AutoDarkModeSvc.exe is running and try again";
                MsgBox msg = new MsgBox(error, AutoDarkModeApp.Properties.Resources.errorOcurredTitle, "error", "close")
                {
                };
                msg.ShowDialog();
                return;
            }
            //handle command line arguments
            if (args.Count > 0)
            {
                Thread.Sleep(1000);
                Mutex.Dispose();
                Mutex = new Mutex(true, "7f326fe1-181c-414f-b7f1-0df4baa578a7");
                Mutex.WaitOne(TimeSpan.FromMilliseconds(100));
                foreach (var value in args)
                {
                    if (value == "/switch")
                    {
                        commandClient.SendMessage(value);
                    }
                    else if (value == "/swap")
                    {
                        commandClient.SendMessage(value);
                    }
                    else if (value == "/dark")
                    {
                        commandClient.SendMessage(value);
                    }
                    else if (value == "/light")
                    {
                        commandClient.SendMessage(value);
                    }
                    else if (value == "/noForce")
                    {
                        commandClient.SendMessage(value);
                    }
                    else if (value == "/update")
                    {
                        var result = commandClient.SendMessageAndGetReply(value);

                        if (result != Response.Err)
                        {
                            Updater updater = new Updater(true);
                            updater.ParseResponse(result);
                        }
                    }
                    else if (value == "/location")
                    {
                        commandClient.SendMessage(Command.Location);
                    }
                    else if (value == "/removeAutostart")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.RemoveAutoStart();
                    }
                    else if (value == "/shutdownService")
                    {
                        commandClient.SendMessage(Command.Shutdown);
                    }
                    else if (value == "/pipeclienttest")
                    {
                        //ICommandClient pc = new PipeClient(Tools.DefaultPipeName);
                        bool result = commandClient.SendMessage(Command.Location);
                        Console.Out.WriteLine(result);
                    }
                    NetMQConfig.Cleanup();
                    Mutex.Dispose();
                    Shutdown();
                }
            }
            else
            {
                MainWindow mainWin = new MainWindow();
                mainWin.Show();
            }
        }

        private void StartService()
        {
            if (!debug)
            {
                
                using Mutex serviceRunning = new Mutex(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
                if (serviceRunning.WaitOne(TimeSpan.FromMilliseconds(100), false))
                {
                    using Process svc = new Process();
                    svc.StartInfo.UseShellExecute = false;
                    svc.StartInfo.FileName = Path.Combine(Extensions.ExecutionDir, "AutoDarkModeSvc.exe");
                    svc.StartInfo.CreateNoWindow = true;
                    svc.Start();
                    serviceRunning.ReleaseMutex();
                }
            }
        }
    }
}