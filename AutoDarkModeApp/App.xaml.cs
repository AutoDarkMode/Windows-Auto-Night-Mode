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
using System.Threading.Tasks;
using AdmProperties = AutoDarkModeApp.Properties;
using System.Management;
using Microsoft.Win32;
using AutoDarkModeApp.Handlers;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        private readonly ICommandClient commandClient = new ZeroMQClient(Address.DefaultPort);
        public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
        private bool debug = false;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Environment.Exit(-1);
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

            bool serviceStartIssued = StartService();
            Task serviceStart = Task.Run(() => WaitForServiceStart());

            MainWindow mainWin = null;
            MainWindowMwpf mainWinMwpf = null;

            int buildNumber = 0;
            try
            {
                using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (registryKey != null)
                {
                    var buildString = registryKey.GetValue("CurrentBuild").ToString();
                    int.TryParse(buildString, out buildNumber);
                }

            }
            catch { }

            if (buildNumber >= 22000)
            {
                mainWinMwpf = new();
            }
            else
            {
                mainWin = new();
            }

            string message = AdmProperties.Resources.StartupLaunchingServiceText;
            MsgBox msg = new(message, AdmProperties.Resources.StartupLaunchingServiceTitle, "info", "none")
            {
                Owner = null
            };
            if (serviceStartIssued)
            {
                msg.Show();
            }
            await serviceStart;
            if (serviceStartIssued)
            {
                Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                msg.Close();
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            if (mainWin != null)
            {
                //ensure auto start is valid
                AutostartHandler.EnsureAutostart();
                mainWin.Show();
            }
            else
            {
                mainWinMwpf.Show();
            }
        }

        private void WaitForServiceStart()
        {
            string heartBeatOK = commandClient.SendMessageWithRetries(Command.Alive, retries: 5);
            if (heartBeatOK == StatusCode.Timeout)
            {
                string error = AdmProperties.Resources.StartupServiceUnresponsive;
                MsgBox msgErr = new(error, AutoDarkModeApp.Properties.Resources.errorOcurredTitle, "error", "close")
                {
                };
                msgErr.ShowDialog();
                return;
            }
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
