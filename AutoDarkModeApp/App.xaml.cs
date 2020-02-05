using AutoDarkModeApp.Communication;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkMode;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using AutoDarkModeSvc;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        private readonly AutoDarkModeConfigBuilder autoDarkModeConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
        private bool debug = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Shutdown();
            }
            bool isClassicMode;
            try
            {
                isClassicMode = autoDarkModeConfigBuilder.Config.ClassicMode;
            }
            catch (Exception)
            {
                isClassicMode = false;
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
            //handle command line arguments
            if (args.Count > 0)
            {
                Mutex.Dispose();
                Mutex = new Mutex(false, "7f326fe1-181c-414f-b7f1-0df4baa578a7");
                Mutex.WaitOne(TimeSpan.FromMilliseconds(100));
                ICommandClient commandClient = new ZeroMQClient(Command.DefaultPort);
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
                    else if (value == "/update")
                    {
                        var result = commandClient.SendMessageAndGetReply(value);

                        if (result != Command.Err)
                        {
                            Updater updater = new Updater();
                            updater.ParseResponse(result);
                        }
                    }
                    else if (value == "/location")
                    {
                        commandClient.SendMessage(Command.Location);
                    }
                    else if (value == "/removeTask")
                    {
                        TaskSchdHandler.RemoveTasks();
                    }
                    else if (value == "/removeAutostart")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.RemoveAutoStart();
                    }
                    else if (value == "/pipeclienttest")
                    {
                        //ICommandClient pc = new PipeClient(Tools.DefaultPipeName);
                        bool result = commandClient.SendMessage(Command.Location);
                        Console.Out.WriteLine(result);
                    }

                    if (isClassicMode) commandClient.SendMessage(Command.Shutdown);
                    NetMQConfig.Cleanup();
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
                if (serviceRunning.WaitOne(TimeSpan.FromMilliseconds(10), false))
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
