﻿using AutoDarkModeComms;
using AutoDarkModeSvc.Communication;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

namespace AutoDarkModeUpdater
{
    class Program
    {
        private static Version Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string holdingDir = Path.Combine(Extensions.UpdateDataDir, "tmp");
        private static readonly ICommandClient client = new ZeroMQClient(Address.DefaultPort);
        private static bool restoreShell;
        private static bool restoreApp;

        static void Main(string[] args)
        {
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
            // Targets where to log to: File and Console
            NLog.Targets.FileTarget logfile = new("logfile")
            {
                FileName = Path.Combine(configDir, "updater.log"),
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${message} ${exception:format=ShortType,Message,Method:separator= > }"
            };
            NLog.Targets.ColoredConsoleTarget logconsole = new("logconsole")
            {
                Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
                "${message} ${exception:format=ShortType,Message,Method:separator= > }"
            };

            NLog.Config.LoggingConfiguration logConfig = new();
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = logConfig;

            Logger.Info($"Auto Dark Mode Updater {Version.Major}.{Version.Minor}.{Version.Build}");

            bool serviceClosed = false;
            try
            {
                Logger.Info("shutting down service");
                string result = client.SendMessageAndGetReply(Command.Shutdown, 5);
                ApiResponse response = ApiResponse.FromString(result);
                if (response.StatusCode == StatusCode.Timeout)
                {
                    serviceClosed = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not cleanly shut down service");
            }

            if (!serviceClosed)
            {
                Logger.Info($"waiting for service to stop");
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        string result = client.SendMessageAndGetReply(Command.Alive, 1);
                        ApiResponse response = ApiResponse.FromString(result);
                        if (response.StatusCode != StatusCode.Timeout)
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "error while waiting for service process to end:");
                }
            }

            try
            {
                Process[] pSvc = Process.GetProcessesByName("AutoDarkModeSvc");
                if (pSvc.Length != 0)
                {
                    Logger.Warn("service still running, force stopping");
                    pSvc[0].Kill();
                }
                Logger.Info("service stop confirmed");

                Process[] pShell = Process.GetProcessesByName("AutoDarkModeShell");
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");
                if (pShell.Length != 0)
                {
                    Logger.Info("stopping shell");
                    pShell[0].Kill();
                    restoreShell = true;
                }
                if (pApp.Length != 0)
                {
                    Logger.Info("stopping app");
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

            if (args.Length > 2)
            {
                if (args[0].Contains("--notify"))
                {
                    restoreShell = args[1].Equals(true.ToString(), StringComparison.Ordinal);
                    restoreApp = args[2].Equals(true.ToString(), StringComparison.Ordinal);
                }
            }

            MoveToTemp();
            CopyNewFiles();
            UpdateInnoInstallerString();
            Cleanup();

            try
            {
                FileVersionInfo newVersion = FileVersionInfo.GetVersionInfo(Extensions.ExecutionPathSvc);
                if (newVersion != null)
                {
                    Logger.Info($"patch complete, installed version: {newVersion.FileVersion}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not read installed version:");
                Logger.Info("patch complete");
            }
            Logger.Info("starting service");
            if (restoreShell) Logger.Info("relaunching shell");
            if (restoreApp) Logger.Info("relaunching app");
            Relaunch(restoreShell, restoreApp, false);
        }

        private static void MoveToTemp()
        {
            // collect all files that are not within the update data directory or the updater itself and the ignore list
            IEnumerable<string> oldFilePaths = Directory.EnumerateFileSystemEntries(Extensions.ExecutionDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains(Extensions.UpdateDataDir) && !f.Contains(Extensions.ExecutionDirUpdater) && !IgnorePaths(f));

            //this operation is dangerous if in the wrong directory, ensure that the AutoDarkModeSvc.exe is in the same directory
            if (!oldFilePaths.Contains(Extensions.ExecutionPathSvc))
            {
                Logger.Error($"patching aborted, wrong directory / service executable not found {Extensions.ExecutionPathSvc}");
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }
            else
            {
                try
                {
                    FileVersionInfo currentVersion = FileVersionInfo.GetVersionInfo(Extensions.ExecutionPathSvc);
                    if (currentVersion != null)
                    {
                        Logger.Info($"currently installed version: {currentVersion.FileVersion}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "could not read installed version:");
                }
            }

            Logger.Info("backing up old files");
            // convert to file info list and move all files into a demporary directory that is a sub directory of the update data dir
            // this is done so the dir can be removed easily once the update is complete
            try
            {
                if (!Directory.Exists(holdingDir))
                {
                    Directory.CreateDirectory(holdingDir);
                }
                foreach (string path in oldFilePaths)
                {
                    if (File.Exists(path))
                    {
                        FileInfo file = new(path);
                        string targetPath = Path.Combine(holdingDir, Path.GetRelativePath(Extensions.ExecutionDir, file.FullName));
                        file.MoveTo(targetPath, true);
                    }
                    else if (Directory.Exists(path))
                    {
                        DirectoryInfo dir = new(path);
                        string targetPath = Path.Combine(holdingDir, Path.GetRelativePath(Extensions.ExecutionDir, dir.FullName));
                        dir.MoveTo(targetPath);
                    }
                    //Logger.Info($"moved file {file.Name} to holding dir");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not move all files to holding dir, attempting rollback:");
                RollbackDir(holdingDir, Extensions.ExecutionDir);
                Cleanup();
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }
        }

        private static void CopyNewFiles()
        {
            Logger.Info("patching auto dark mode");
            // move new files from unpack directory to assembly path
            string unpackDirectory = Path.Combine(Extensions.UpdateDataDir, "unpacked");


            IEnumerable<string> paths = Directory.EnumerateFileSystemEntries(unpackDirectory, "*.*", SearchOption.TopDirectoryOnly);
            try
            {
                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        FileInfo file = new(path);
                        string targetPath = Path.Combine(Extensions.ExecutionDir, Path.GetRelativePath(unpackDirectory, file.FullName));
                        file.MoveTo(targetPath, true);
                    }
                    else if (Directory.Exists(path))
                    {
                        DirectoryInfo dir = new(path);
                        string targetPath = Path.Combine(Extensions.ExecutionDir, Path.GetRelativePath(unpackDirectory, dir.FullName));
                        dir.MoveTo(targetPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not move all files, attempting rollback:");
                RollbackDir(holdingDir, Extensions.ExecutionDir);
                Cleanup();
                Relaunch(restoreShell, restoreApp, true);
                Environment.Exit(-1);
            }
        }

        private static void UpdateInnoInstallerString()
        {
            Logger.Info("updating setup version string");
            try
            {
                using RegistryKey innoInstallerKey = Registry.Users.OpenSubKey($"{SID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{470BC918-3740-4A97-9797-8570A7961130}}_is1", true);
                if (innoInstallerKey == null)
                {
                    Logger.Info("inno installer not detected, assuming portable adm installation");
                }
                else
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(Extensions.ExecutionDir, "AutoDarkModeSvc.exe"));
                    innoInstallerKey.SetValue("DisplayVersion", versionInfo.FileVersion);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not update installer version string:");
            }
        }

        private static void Cleanup()
        {
            // delete old files
            try
            {
                Directory.Delete(Extensions.UpdateDataDir, true);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not delete holding dir, please investigate manually:");
            }
        }

        private static SecurityIdentifier SID
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User;
            }
        }

        private static void Relaunch(bool restoreShell, bool restoreApp, bool failed)
        {
            try
            {
                using Process svc = new();
                svc.StartInfo.UseShellExecute = false;
                svc.StartInfo.FileName = Path.Combine(Extensions.ExecutionPathSvc);
                _ = svc.Start();
                if (restoreApp)
                {
                    using Process app = new();
                    app.StartInfo.UseShellExecute = false;
                    app.StartInfo.FileName = Path.Combine(Extensions.ExecutionPathApp);
                    _ = app.Start();
                }
                if (restoreShell)
                {
                    using Process shell = new();
                    shell.StartInfo.UseShellExecute = false;
                    shell.StartInfo.FileName = Path.Combine(Extensions.ExecutionPathShell);
                    _ = shell.Start();
                }

                if (failed)
                {
                    if (client.SendMessageWithRetries(Command.UpdateFailed, retries: 5) == StatusCode.Timeout)
                    {
                        Logger.Warn("could not send failed update message due to service not starting in time");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while restarting auto dark mode");
            }

        }

        private static void RollbackDir(string source, string target)
        {
            try
            {
                PrepareRollback();

                IEnumerable<string> holdingPaths = Directory.EnumerateFileSystemEntries(source, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var path in holdingPaths)
                {
                    if (File.Exists(path))
                    {
                        FileInfo file = new(path);
                        string targetPath = Path.Combine(target, Path.GetRelativePath(source, file.FullName));
                        file.MoveTo(targetPath, true);
                        Logger.Info($"rolled back file {file.Name} to default dir {target}");
                    }
                    else if (Directory.Exists(path))
                    {
                        DirectoryInfo dir = new(path);
                        string targetPath = Path.Combine(target, Path.GetRelativePath(source, dir.FullName));
                        dir.MoveTo(targetPath);
                        Logger.Info($"rolled back directory {dir.Name} to {targetPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "rollback failed, this is non-recoverable, please reinstall auto dark mode:");
                Environment.Exit(-2);
            }
            Logger.Info("rollback successful, no update has been performed, restarting auto dark mode");
        }

        private static void PrepareRollback()
        {
            Logger.Info("preparing main directory for rollback");
            IEnumerable<string> filePaths = Directory.EnumerateFileSystemEntries(Extensions.ExecutionDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains(Extensions.UpdateDataDir) && !f.Contains(Extensions.ExecutionDirUpdater) && !IgnorePaths(f));

            foreach (string path in filePaths)
            {
                Logger.Info($"deleting {path}");
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static bool IgnorePaths(string path)
        {
            if (path.Contains("unins000.exe"))
            {
                return true;
            }
            if (path.Contains("unins000.dat"))
            {
                return true;
            }
            if (path.Contains("AutoDarkMode.VisualElementsManifest.xml"))
            {
                return true;
            }
            return false;
        }
    }
}
