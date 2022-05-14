using AutoDarkModeSvc.Monitors;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;

namespace AutoDarkModeSvc
{
    static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
        private static Service Service { get; set; }

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

            // Targets where to log to: File and Console
            NLog.Targets.FileTarget logfile = new("logfile")
            {
                FileName = Path.Combine(configDir, "service.log"),
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:format=ShortType,Message,Method:separator= > }"
            };
            NLog.Targets.ColoredConsoleTarget logconsole = new("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception}"
            };


            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            if (argsList.Contains("/debug"))
            {
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            }
            else
            {
                config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
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
                string commitHash = Extensions.CommitHash();
                if (commitHash != "")
                {
                    Logger.Info($"commit hash: {commitHash}, build: {Assembly.GetExecutingAssembly().GetName().Version}");
                    Logger.Info($"cwd: {Extensions.ExecutionPath}");
                }
                else
                {
                    Logger.Error("could not retrieve commit hash");
                }
                //Instantiate Runtime config
                GlobalState state = GlobalState.Instance();

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
                        AdmConfigBuilder.BackUpConfig();
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
                if (builder.Config.Tunable.Debug && !argsList.Contains("/debug"))
                {
                    config = new NLog.Config.LoggingConfiguration();
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                    LogManager.Configuration = config;
                }

                Logger.Debug("config file loaded");

                //if a path is set to null, set it to the currently actvie theme for convenience reasons
                bool configUpdateNeeded = false;
                if (builder.Config.WindowsThemeMode.Enabled)
                {
                    if (!File.Exists(builder.Config.WindowsThemeMode.DarkThemePath) || builder.Config.WindowsThemeMode.DarkThemePath == null)
                    {
                        builder.Config.WindowsThemeMode.DarkThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                            + @"\Microsoft\Windows\Themes", state.CurrentWindowsThemeName + ".theme");
                        configUpdateNeeded = true;
                    }
                    if (!File.Exists(builder.Config.WindowsThemeMode.DarkThemePath) || builder.Config.WindowsThemeMode.LightThemePath == null)
                    {
                        builder.Config.WindowsThemeMode.LightThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                           + @"\Microsoft\Windows\Themes", state.CurrentWindowsThemeName + ".theme");
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

                timerMillis = (timerMillis == 0) ? TimerFrequency.Short : timerMillis;
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Service = new Service(timerMillis);
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
