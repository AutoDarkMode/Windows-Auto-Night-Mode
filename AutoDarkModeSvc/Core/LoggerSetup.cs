using AutoDarkModeLib;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Core
{
    public static class LoggerSetup
    {
        private static string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Targets where to log to: File and Console
        public static NLog.Targets.FileTarget Logfile { get; } = new("logfile")
        {
            FileName = Path.Combine(configDir, "service.log"),
            Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
            "${callsite:includeNamespace=False:" +
            "cleanNamesOfAnonymousDelegates=true:" +
            "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:format=ShortType,Message,Method:separator= > }",
            KeepFileOpen = false
        };
        public static NLog.Targets.ColoredConsoleTarget Logconsole { get; } = new("logconsole")
        {
            Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
            "${callsite:includeNamespace=False:" +
            "cleanNamesOfAnonymousDelegates=true:" +
            "cleanNamesOfAsyncContinuations=true}: ${message} ${exception}"
        };

        public static void UpdateLogmanConfig()
        {
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            var config = new NLog.Config.LoggingConfiguration();
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, Logconsole);
            if (builder.Config.Tunable.Debug)
            {
                if (builder.Config.Tunable.Trace)
                {
                    Logger.Info("enabling trace logs");
                    config.AddRule(LogLevel.Trace, LogLevel.Fatal, Logfile);
                }
                else
                {
                    Logger.Info("enabling debug logs");
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, Logfile);
                }
            }
            else
            {
                Logger.Info("enabling standard logging");
                config.AddRule(LogLevel.Info, LogLevel.Fatal, Logfile);
            }
            LogManager.Configuration = config;
        }
    }
}
