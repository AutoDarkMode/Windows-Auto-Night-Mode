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

            OperatingSystem os = Environment.OSVersion;
            MainWindow mainWin = null;
            MainWindowMwpf mainWinMwpf = null;
            string osVersionString = GetOsVer();
            Version osVersion = new();
            if (osVersionString != string.Empty)
            {
                try
                {
                    string[] split = osVersionString.Split(".");
                    osVersion = new(major: int.Parse(split[0]), minor: int.Parse(split[1]), build: int.Parse(split[2]));
                }
                catch { }
            }
            if (osVersion.Major > 10)
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
            bool serviceStartIssued = StartService();
            if (serviceStartIssued)
            {
                msg.Show();
            }

            await Task.Run(() => WaitForServiceStart());
            if (serviceStartIssued)
            {
                Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                msg.Close();
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            if (mainWin != null)
            {
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


        private static ManagementObject GetMngObj(string className)
        {
            var wmi = new ManagementClass(className);

            foreach (var o in wmi.GetInstances())
            {
                var mo = (ManagementObject)o;
                if (mo != null) return mo;
            }

            return null;
        }

        public static string GetOsVer()
        {
            try
            {
                ManagementObject mo = GetMngObj("Win32_OperatingSystem");

                return null == mo ? string.Empty : mo["Version"] as string;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
