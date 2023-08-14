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
using AutoDarkModeSvc.Monitors;
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Reflection;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc
{
    static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
        private static Service Service { get; set; }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SystemParametersInfo(int uAction, int uParam, int lpvParam, int fuWinIni);

        private const int SPI_SETKEYBOARDCUES = 4107; //100B
        private const int SPIF_SENDWININICHANGE = 2;

        public static BlockingCollection<Action> ActionQueue = new();
        private static Thread queueThread;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            List<string> argsList = args.Length > 0 ? new List<string>(args) : new List<string>();
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");

            //Set up Logger
            NLog.Config.LoggingConfiguration config = new();

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, LoggerSetup.Logconsole);
            if (argsList.Contains("/debug"))
            {
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, LoggerSetup.Logfile);
            }
            else if (argsList.Contains("/trace"))
            {
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, LoggerSetup.Logfile);
            }
            else
            {
                config.AddRule(LogLevel.Info, LogLevel.Fatal, LoggerSetup.Logfile);
            }
            // Apply config
            LogManager.Configuration = config;

            try { _ = Directory.CreateDirectory(configDir); }
            catch (Exception e) { Logger.Fatal(e, "could not create config directory"); }

            try
            {
                if (!mutex.WaitOne(500, false))
                {
                    Logger.Debug("app instance already open");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed getting mutex, " + ex.Message);
                return;
            }

            try
            {
                string commitHash = Helper.CommitHash();
                if (commitHash != "")
                {
                    Logger.Info($"commit hash: {commitHash}, build: {Assembly.GetExecutingAssembly().GetName().Version}");
                    Logger.Info($"cwd: {Helper.ExecutionPath}");
                }
                else
                {
                    Logger.Error("could not retrieve commit hash");
                }

                //Populate configuration
                AdmConfigBuilder builder = AdmConfigBuilder.Instance();
                try
                {
                    builder.Load();
                    Logger.Debug("config builder instantiated and configuration loaded");
                }
                catch (Exception e)
                {
                    Logger.Error(e, "could not read config file, resetting config file:");
                    try
                    {
                        AdmConfigBuilder.MakeConfigBackup();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "could not create config file backup, overwriting");
                    }
                    try
                    {
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex, "could not create empty config file, this is a fatal error. exiting...");
                        LogManager.Shutdown();
                        Environment.Exit(-1);
                    }
                }

                try
                {
                    builder.LoadScriptConfig();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"script configuration could not be loaded. All scripts inactive:");
                }

                try
                {
                    builder.LoadLocationData();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not load location data:");
                }

                // modify config if debug flag is set in config
                if (!argsList.Contains("/debug") && !argsList.Contains("/trace") && (builder.Config.Tunable.Debug || builder.Config.Tunable.Trace))
                {
                    config = new NLog.Config.LoggingConfiguration();
                    config.AddRule(LogLevel.Trace, LogLevel.Fatal, LoggerSetup.Logconsole);
                    if (builder.Config.Tunable.Trace)
                    {
                        config.AddRule(LogLevel.Trace, LogLevel.Fatal, LoggerSetup.Logfile);
                    }
                    else if (builder.Config.Tunable.Debug)
                    {
                        config.AddRule(LogLevel.Debug, LogLevel.Fatal, LoggerSetup.Logfile);
                    }
                    LogManager.Configuration = config;
                }

                Logger.Debug("config file loaded");

                // Instantiate Runtime config
                GlobalState state = GlobalState.Instance();
                state.RefreshThemes(builder.Config);


                // if a path is set to null, set it to the currently actvie theme for convenience reasons
                bool configUpdateNeeded = false;
                if (builder.Config.WindowsThemeMode.Enabled)
                {
                    if (!File.Exists(builder.Config.WindowsThemeMode.DarkThemePath) || builder.Config.WindowsThemeMode.DarkThemePath == null)
                    {
                        builder.Config.WindowsThemeMode.DarkThemePath = state.UnmanagedActiveThemePath;
                        configUpdateNeeded = true;
                    }
                    if (!File.Exists(builder.Config.WindowsThemeMode.LightThemePath) || builder.Config.WindowsThemeMode.LightThemePath == null)
                    {
                        builder.Config.WindowsThemeMode.LightThemePath = state.UnmanagedActiveThemePath;
                        configUpdateNeeded = true;
                    }
                    if (configUpdateNeeded)
                    {
                        Logger.Warn("one or more theme paths not set at program start, reinstantiation needed");
                        try
                        {
                            builder.Save();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "couldn't save configuration file");
                        }
                    }


                }

                int timerMillis = 0;
                if (args.Length != 0)
                {
                    bool success = int.TryParse(args[0], out timerMillis);
                    if (success) Logger.Info($"main timer override to {timerMillis} seconds");
                }

                queueThread = new Thread(() =>
                {
                    while (ActionQueue.TryTake(out Action a, -1))
                    {
                        try
                        {
                            Logger.Debug($"action queue invoking ${a.Method.Name}");
                            a.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "error running method on queue thread:");
                        }
                    }
                });
                queueThread.Start();

                ActionQueue.Add(() =>
                {
                    // Listen to toast notification activation
                    Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.OnActivated += toastArgs =>
                    {
                        ToastHandler.HandleToastAction(toastArgs);
                    };
                });
                
                if (timerMillis != 0)
                {
                    TimerFrequency.Main = timerMillis;
                }
                else
                {
                    timerMillis = TimerFrequency.Main;
                }
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Service = new Service(timerMillis);
                Service.Text = "Auto Dark Mode";

                /* Disable for now.
                try
                {
                    // always show accelerator underlines
                    _ = SystemParametersInfo(SPI_SETKEYBOARDCUES, 0, 1, 0);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "could not set access key highlight flag");
                }
                */

                Application.Run(Service);

            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "unhandled exception causing panic");
            }
            finally
            {
                ActionQueue.CompleteAdding();
                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
                Logger.Info("service shutdown successful");
                LogManager.Shutdown();
                mutex.Dispose();
            }
        }
    }
}
