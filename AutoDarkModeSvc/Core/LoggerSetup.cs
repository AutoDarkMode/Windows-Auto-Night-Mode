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
using System;
using System.IO;
using AutoDarkModeLib;
using NLog;

namespace AutoDarkModeSvc.Core
{
    public static class LoggerSetup
    {
        private static string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Targets where to log to: File and Console
        public static NLog.Targets.FileTarget Logfile
        {
            get;
        } = new("logfile")
        {
            FileName = Path.Combine(configDir, "service.log"),
            Layout = @"${date:format=yyyy-MM-dd HH\:mm\:ss} | ${level} | " +
            "${callsite:includeNamespace=False:" +
            "cleanNamesOfAnonymousDelegates=true:" +
            "cleanNamesOfAsyncContinuations=true}: ${message} ${exception:format=ShortType,Message,Method:separator= > }",
            KeepFileOpen = false
        };
        public static NLog.Targets.ColoredConsoleTarget Logconsole
        {
            get;
        } = new("logconsole")
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
