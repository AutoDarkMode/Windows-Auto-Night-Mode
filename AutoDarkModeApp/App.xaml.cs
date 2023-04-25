#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeComms;
using AutoDarkModeLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using AutoDarkModeSvc.Communication;
using System.Threading.Tasks;
using AutoDarkModeApp.Handlers;
using System.Windows.Shell;
using System.Globalization;
using AdmProperties = AutoDarkModeLib.Properties;
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
                Environment.Exit(-1);
            }

            if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                Environment.Exit(-1);
            }

            // must be initialized first, before any other message boxes, otherwise the window won't be displayed
            MainWindow mainWin = null;
            MainWindowMwpf mainWinMwpf = null;

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                mainWinMwpf = new();
            }
            else
            {
                mainWin = new();
            }

            try
            {
                AdmConfigBuilder builder = AdmConfigBuilder.Instance();
                builder.Load();
                if (Settings.Default.Language != builder.Config.Tunable.UICulture)
                {
                    builder.Config.Tunable.UICulture = Settings.Default.Language;
                    builder.Save();
                }
            }
            catch (Exception ex)
            {
                string error = "failed initializing UI culture\n" + ex.Message;
                Dispatcher.Invoke(() =>
                {
                    ErrorMessageBoxes.ShowErrorMessage(ex, null, "Startup", "Failed to force-set UI culture");
                });
            }

            bool serviceStartIssued = StartService();
            Task serviceStart = Task.Run(() => WaitForServiceStart());

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
                    MsgBox msgErr = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "close")
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
                    svc.StartInfo.FileName = Path.Combine(Helper.ExecutionDir, "AutoDarkModeSvc.exe");
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
                sysFormat = sysFormat[..sysFormat.IndexOf(":")];
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    if (Settings.Default.FirstRun) Settings.Default.AlterTime = true;
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
