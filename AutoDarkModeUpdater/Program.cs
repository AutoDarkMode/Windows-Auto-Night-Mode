using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoDarkModeComms;
using AutoDarkModeSvc.Communication;
using NLog;
using Windows.UI.Notifications;

namespace AutoDarkModeUpdater
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string holdingDir = Path.Combine(Extensions.UpdateDataDir, "tmp");
        private static readonly ICommandClient client = new ZeroMQClient(Address.DefaultPort);

        static void Main(string[] args)
        {
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
            // Targets where to log to: File and Console
            NLog.Targets.FileTarget logfile = new("logfile")
            {
                FileName = Path.Combine(configDir, "updater.log"),
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:separator=|}"
            };
            NLog.Targets.ColoredConsoleTarget logconsole = new("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${callsite:includeNamespace=False:" +
                "cleanNamesOfAnonymousDelegates=true:" +
                "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:separator=|}"
            };

            NLog.Config.LoggingConfiguration logConfig = new();
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = logConfig;

            Logger.Info("Auto Dark Mode Updater v1.11");

            bool restoreShell = false;
            bool restoreApp = false;

            try
            {
                Process[] pShell = Process.GetProcessesByName("AutoDarkModeShell");
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");
                if (pShell.Length != 0)
                {
                    pShell[0].Kill();
                    restoreShell = true;
                }
                if (pApp.Length != 0)
                {
                    pApp[0].Kill();
                    restoreApp = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "other auto dark mode components still running, skipping update");
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-2);
            }

            try
            {
                string result = client.SendMessageAndGetReply(Command.Shutdown);
                ApiResponse response = ApiResponse.FromString(result);
                if (response.StatusCode != StatusCode.Ok && response.StatusCode != StatusCode.Timeout)
                {
                    throw new Exception("error shutting down service, aborting update");
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "could not shut down service, aborting upgrade");
                Environment.Exit(-1);
            }

            string admDir = Extensions.ExecutionDir;
            if (args.Length > 2)
            {
                if (args[0].Contains("--notify"))
                {
                    restoreShell = args[1].Equals(true.ToString());
                    restoreApp = args[2].Equals(true.ToString());
                }
            }

            // move old files out
            // collect all files that are not within the update data directory or the updater itself
            IEnumerable<string> oldFilePaths = Directory.GetFiles(Extensions.ExecutionDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Extensions.UpdateDataDir) && !f.Contains(Extensions.ExecutionDirUpdater));

            //this operation is dangerous if in the wrong directory, ensure that the AutoDarkModeSvc.exe is in the same directory
            if (!oldFilePaths.Contains(Extensions.ExecutionPath))
            {
                Logger.Error($"updated aborted, wrong directory / service executable not found {Extensions.ExecutionPath}");
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }

            // convert to file info list and move all files into a demporary directory that is a sub directory of the update data dir
            // this is done so the dir can be removed easily once the update is complete
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
                Logger.Error(ex, "could not move all files to holding dir, attempting rollback:");
                RollbackDir(holdingDir, Extensions.ExecutionDir);
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }


            // move new files from unpack directory to assembly path
            string unpackDirectory = Path.Combine(Extensions.UpdateDataDir, "unpacked");
            IEnumerable<FileInfo> files = Directory.GetFiles(unpackDirectory, "*.*", SearchOption.AllDirectories).Select(f => new FileInfo(f));
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
                Logger.Error(ex, "could not move all files, attempting rollback:");
                RollbackDir(holdingDir, Extensions.ExecutionDir);
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }

            try
            {
                Directory.Delete(Extensions.UpdateDataDir, true);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not delete holding dir, please investigate manually:");
            }

            Logger.Info("update complete, starting service");
            Relaunch(restoreShell, restoreApp, false);
        }

        private static void Relaunch(bool restoreShell, bool restoreApp, bool failed)
        {
            Process.Start(Extensions.ExecutionPath);
            if (restoreApp)
            {
                Process.Start(Extensions.ExecutionPathApp);
            }
            if (restoreShell)
            {
                Process.Start(Extensions.ExecutionPathShell);
            }

            if (failed)
            {
                client.SendMessage(Command.UpdateFailed);
            }
        }

        private static void RollbackDir(string source, string target)
        {
            IEnumerable<FileInfo> holdingFiles = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Select(f => new FileInfo(f));
            try
            {
                foreach (var file in holdingFiles)
                {
                    file.MoveTo(Path.Combine(target, file.Name), true);
                    Logger.Info($"rolled back file {file.Name} to default dir {target}");
                }
            }
            catch (Exception ex) {
                Logger.Fatal(ex, "rollback failed this is non-recoverable, please reinstall auto dark mode:");
                Environment.Exit(-2);
            }
            Logger.Info("rollback successful, no update has been performed, restarting auto dark mode");
        }
    }
}
