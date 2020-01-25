using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Timers;
using NLog;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AutoDarkModeSvc
{
    static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
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
                try
                {
                    Directory.CreateDirectory(configDir);
                }
                catch (Exception e)
                {
                    Logger.Debug(e, "could not create config directory");
                }

                // Targets where to log to: File and Console
                var logfile = new NLog.Targets.FileTarget("logfile")
                {
                    FileName = Path.Combine(configDir, "service.log"),
                    Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | ${callsite:includeNamespace=False}: ${message} ${exception:separator=|}"
                };
                var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
                {
                    Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | ${callsite:includeNamespace=False}: ${message} ${exception:separator=|}"
                };

                // Rules for mapping loggers to targets            
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

                // Apply config           
                LogManager.Configuration = config;


                if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
                {
                    Logger.Debug("app instance already open");
                    return;
                }

                //Instantiate Runtime config
                RuntimeConfig.Instance();

                //Populate configuration
                AutoDarkModeConfigBuilder Builder = AutoDarkModeConfigBuilder.Instance();
                try
                {
                    Builder.Load();
                    Logger.Debug("config builder instantiated and configuration loaded");
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "could not read config file. shutting down application!");
                    NLog.LogManager.Shutdown();
                    Application.Exit();
                }

                int timerMillis = 0;
                if (args.Length != 0)
                {
                    Int32.TryParse(args[0], out timerMillis);
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
