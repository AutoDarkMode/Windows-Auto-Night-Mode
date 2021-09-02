using AutoDarkModeSvc.Config;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoDarkModeSvc
{
    static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new Mutex(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
        private static Service Service { get; set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
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

                if (Builder.Config.Tunable.Debug && !argsList.Contains("/debug")) {
                    config = new NLog.Config.LoggingConfiguration();
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                    LogManager.Configuration = config;
                }

                Logger.Debug("config file loaded");

                //if a path is set to null, set it to the currently actvie theme for convenience reasons
                bool configUpdateNeeded = false;
                if (Builder.Config.WindowsThemeMode)
                {
                    if (!File.Exists(Builder.Config.DarkThemePath) || Builder.Config.DarkThemePath == null)
                    {
                        Builder.Config.DarkThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                            + @"\Microsoft\Windows\Themes", ThemeHandler.GetCurrentThemeName() + ".theme");
                        configUpdateNeeded = true;
                    }
                    if (!File.Exists(Builder.Config.DarkThemePath) || Builder.Config.LightThemePath == null)
                    {
                        Builder.Config.LightThemePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
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
                    int.TryParse(args[0], out timerMillis);
                }
                timerMillis = (timerMillis == 0) ? TimerFrequency.Short : timerMillis;
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Service = new Service(timerMillis);
                Application.Run(Service);
            }
            finally 
            {
                //clean shutdown
                if (Service != null) {
                    Service.Cleanup();
                }
                mutex.Dispose();
            }
        }
    }
}
