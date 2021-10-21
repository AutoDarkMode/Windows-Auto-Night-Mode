using AutoDarkModeComms;
using AutoDarkModeConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using AutoDarkModeSvc.Communication;
using System.Threading.Tasks;
using AdmProperties = AutoDarkModeApp.Properties;
using Microsoft.Win32;
using AutoDarkModeApp.Handlers;
using System.Windows.Shell;
using System.Globalization;
using AutoDarkModeApp.Properties;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
        private bool debug = false;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
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

            if (args.Count > 0)
            {
                IMessageClient client = new PipeClient();
                _ = client.SendMessageAndGetReply(args[0]);
            }
            
            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Environment.Exit(-1);
            }      

            bool serviceStartIssued = StartService();
            Task serviceStart = Task.Run(() => WaitForServiceStart());

            MainWindow mainWin = null;
            MainWindowMwpf mainWinMwpf = null;

            if (Environment.OSVersion.Version.Build >= 22000)
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
            if (!Settings.Default.FirstRun && (Settings.Default.Left != 0 || Settings.Default.Top != 0))
            {
                msg.Left = Settings.Default.Left;
                msg.Top = Settings.Default.Top;
            }
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


            //only run at first startup
            if (Settings.Default.FirstRun)
            {
                //enable autostart
                AutostartHandler.EnableAutoStart(null);

                //check if system uses 12 hour clock
                SystemTimeFormat();

                //create jump list entries
                AddJumpList();

                //finished first startup code
                Settings.Default.FirstRun = false;
            }
            else
            {
                // validate autostart always if firstrun has completed
                AutostartHandler.EnsureAutostart(null);
            }

            //run if user changed language in previous session
            if (Settings.Default.LanguageChanged)
            {
                AddJumpList();
                Settings.Default.LanguageChanged = false;
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
            string heartBeatOK = MessageHandler.Client.SendMessageWithRetries(Command.Alive, retries: 5);
            if (heartBeatOK == StatusCode.Timeout)
            {
                string error = AdmProperties.Resources.StartupServiceUnresponsive;
                Dispatcher.Invoke(() =>
                {
                    MsgBox msgErr = new(error, AutoDarkModeApp.Properties.Resources.errorOcurredTitle, "error", "close")
                    {
                    };
                    _ = msgErr.ShowDialog();
                });
                Environment.Exit(-2);
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

        private static void SystemTimeFormat()
        {
            try
            {
                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                sysFormat = sysFormat.Substring(0, sysFormat.IndexOf(":"));
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    AdmProperties.Settings.Default.AlterTime = true;
                }
            }
            catch
            {

            }
        }

        //jump list
        private static void AddJumpList()
        {
            JumpTask darkJumpTask = new()
            {
                //Dark theme
                Title = AdmProperties.Resources.lblDarkTheme,
                Arguments = Command.Dark,
                CustomCategory = AdmProperties.Resources.lblSwitchTheme
            };
            JumpTask lightJumpTask = new()
            {
                //Light theme
                Title = AdmProperties.Resources.lblLightTheme,
                Arguments = Command.Light,
                CustomCategory = AdmProperties.Resources.lblSwitchTheme
            };
            JumpTask resetJumpTask = new()
            {
                //Reset
                Title = AdmProperties.Resources.lblReset,
                Arguments = Command.NoForce,
                CustomCategory = AdmProperties.Resources.lblSwitchTheme
            };

            JumpList jumpList = new();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.JumpItems.Add(resetJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

            JumpList.SetJumpList(Current, jumpList);
        }

    }
}
