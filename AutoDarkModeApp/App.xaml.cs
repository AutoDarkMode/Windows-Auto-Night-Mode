using AutoDarkModeComms;
using AutoDarkModeConfig;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        private readonly ICommandClient commandClient = new ZeroMQClient(Address.DefaultPort);
        public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
        private bool debug = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Shutdown();
            }

            List<string> args = new();
            if (e.Args.Length > 0)
            {
                args = new List<string>(e.Args);
            }
            if (args.Count > 0 && args.Contains("/debug"))
            {
                debug = true;
                args.Remove("/debug");
            }

            MainWindow mainWin = new();

            string message = "Service not running yet, starting service";
            MsgBox msg = new(message, "Launching Service", "info", "none")
            {
                Owner = null
            };
            bool serviceStartIssued = StartService();
            if (serviceStartIssued)
            {
                msg.Show();
            }
            string heartBeatOK = commandClient.SendMessageWithRetries(Command.Alive, retries: 5);
            if (heartBeatOK == StatusCode.Timeout)
            {
                string error = "Backend not responding. Check if AutoDarkModeSvc.exe is running and try again";
                MsgBox msgErr = new(error, AutoDarkModeApp.Properties.Resources.errorOcurredTitle, "error", "close")
                {
                };
                msgErr.ShowDialog();
                return;
            }
            if (serviceStartIssued)
            {
                Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                msg.Close();
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            mainWin.Show();
        }

        private bool StartService()
        {
            if (!debug)
            {

                using Mutex serviceRunning = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
                if (serviceRunning.WaitOne(TimeSpan.FromMilliseconds(100), false))
                {
                    using Process svc = new();
                    svc.StartInfo.UseShellExecute = false;
                    svc.StartInfo.FileName = Path.Combine(Extensions.ExecutionDir, "AutoDarkModeSvc.exe");
                    svc.StartInfo.CreateNoWindow = true;
                    svc.Start();
                    serviceRunning.ReleaseMutex();
                    return true;
                }
            }
            return false;
        }
    }
}
