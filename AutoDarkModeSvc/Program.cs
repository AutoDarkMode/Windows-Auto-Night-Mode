using AutoDarkModeSvc.Config;
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
            //Set up Logger
            var config = new NLog.Config.LoggingConfiguration();
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = Path.Combine(configDir, "service.log"),
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:separator=|}"
            };
            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:separator=|}"
            };

            List<string> argsList;
            if (args.Length > 0)
            {
                argsList = new List<string>(args);
            }
            else
            {
                argsList = new List<string>();
            }

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

            try
            {
                Directory.CreateDirectory(configDir);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "could not create config directory");
            }

            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
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
                    Logger.Info($"commit hash: {commitHash}");
                }
                else
                {
                    Logger.Error("could not retrieve commit hash");
                }
                //Instantiate Runtime config
                GlobalState.Instance();

                //Populate configuration
                AdmConfigBuilder Builder = AdmConfigBuilder.Instance();
                try
                {
                    Builder.Load();
                    Builder.LoadLocationData();
                    Logger.Debug("config builder instantiated and configuration loaded");
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "could not read config file. shutting down application!");
                    LogManager.Shutdown();
                    Environment.Exit(-1);
                }

                if (Builder.Config.Tunable.Debug && !argsList.Contains("/debug"))
                {
                    config = new NLog.Config.LoggingConfiguration();
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                    LogManager.Configuration = config;
                }

                Logger.Debug("config file loaded");

                //if a path is set to null, set it to the currently actvie theme for convenience reasons
                bool configUpdateNeeded = false;
                if (Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (!File.Exists(Builder.Config.WindowsThemeMode.DarkThemePath) || Builder.Config.WindowsThemeMode.DarkThemePath == null)
                    {
                        Builder.Config.WindowsThemeMode.DarkThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                            + @"\Microsoft\Windows\Themes", ThemeHandler.GetCurrentThemeName() + ".theme");
                        configUpdateNeeded = true;
                    }
                    if (!File.Exists(Builder.Config.WindowsThemeMode.DarkThemePath) || Builder.Config.WindowsThemeMode.LightThemePath == null)
                    {
                        Builder.Config.WindowsThemeMode.LightThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                           + @"\Microsoft\Windows\Themes", ThemeHandler.GetCurrentThemeName() + ".theme");
                        configUpdateNeeded = true;
                    }
                    if (configUpdateNeeded)
                    {
                        Logger.Warn("one or more theme paths not set at program start, reinstantiation needed");
                        try
                        {
                            Builder.Save();
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
                    Logger.Info($"main timer override to {timerMillis} seconds");
                    _ = int.TryParse(args[0], out timerMillis);
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
                //clean shutdown
                if (Service != null)
                {
                    Service.Cleanup();
                }
                try
                {
                    System.Diagnostics.Process[] pApp = System.Diagnostics.Process.GetProcessesByName("AutoDarkModeApp");
                    if (pApp.Length != 0)
                    {
                        pApp[0].Kill();
                    }
                    foreach (System.Diagnostics.Process p in pApp)
                    {
                        p.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "couldn't close app before shutting down service");
                }
                ActionQueue.CompleteAdding();
                Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
                mutex.Dispose();
            }
        }
    }
}
