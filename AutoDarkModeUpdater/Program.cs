using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using Windows.UI.Notifications;

namespace AutoDarkModeUpdater
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = Path.Combine(configDir, "updater.log"),
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

            var logConfig = new NLog.Config.LoggingConfiguration();
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = logConfig;


            string admDir = Extensions.ExecutionDir;
            bool notifyAboutUpdate = false;
            if (args.Length > 0)
            {
                if (args[0].Contains("notify"))
                {
                    notifyAboutUpdate = true;
                }
            }
            if (notifyAboutUpdate)
            {
                var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
                var text = xml.GetElementsByTagName("text");
                text[0].AppendChild(xml.CreateTextNode("Auto Dark Mode is updating"));
                text[1].AppendChild(xml.CreateTextNode("Please wait until the update is complete"));
                var toast = new ToastNotification(xml);
                ToastNotificationManager.CreateToastNotifier("AutoDarkModeSvc").Show(toast);
            }

            // move old files out
            string holdingDir = Path.Combine(Extensions.UpdateDataDir, "tmp");
            IEnumerable<string> oldFilePaths = Directory.GetFiles(Extensions.ExecutionDir).Where(f => !f.Contains(Extensions.UpdateDataDir));
            if (!oldFilePaths.Contains(Extensions.ExecutionPath))
            {
                Logger.Fatal($"wrong directory /service executable not found {Extensions.ExecutionPath}");
                Environment.Exit(-1);
            }
            IEnumerable<FileInfo> oldFiles = oldFilePaths.Select(f => new FileInfo(f));
            try
            {
                if (!Directory.Exists(holdingDir))
                {
                    Directory.CreateDirectory(holdingDir);
                }
                foreach (var file in oldFiles)
                {
                    file.MoveTo(Path.Combine(holdingDir, file.Name), true);
                    Logger.Info($"moved file {file.Name} to holding dir");
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "could not move all files to holding dir, fatal, please reinstall Auto Dark Mode");
                Environment.Exit(-1);
            }


            // move new files from unpack directory to assembly path
            string unpackDirectory = Path.Combine(Extensions.UpdateDataDir, "unpacked");
            IEnumerable<FileInfo> files = Directory.GetFiles(unpackDirectory).Select(f => new FileInfo(f));
            try
            {
                foreach (var file in files)
                {
                    file.MoveTo(Path.Combine(admDir, file.Name), true);
                    Logger.Info($"updated file {file.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "could not move all files, fatal, please reinstall Auto Dark Mode");
                Environment.Exit(-1);
            }

            try
            {
                Directory.Delete(holdingDir, true);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not delete holding dir, please investigate manually");
            }

            Logger.Info("update complete, starting service");
            Process.Start(Extensions.ExecutionPath);
        }
    }
}
