using NLog;
using System;
using System.Windows.Forms;

namespace AutoDarkModeSvc
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //Set up Logger
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "service.log" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            //config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;

            int timerMillis = 0;
            if (args.Length != 0)
            {
                Int32.TryParse(args[0], out timerMillis);
            }
            timerMillis = (timerMillis == 0) ? 10000 : timerMillis;
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Service(timerMillis));
        }
    }
}
