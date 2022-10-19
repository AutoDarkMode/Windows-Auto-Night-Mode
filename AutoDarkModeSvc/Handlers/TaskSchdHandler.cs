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
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;

namespace AutoDarkModeSvc.Handlers
{
    public static class TaskSchdHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static readonly string logon = "ADM Logon";
        static readonly string folder = "ADM_" + Environment.UserName;
        static readonly string author = "Armin Osaj";
        static readonly string program = "Windows Auto Dark Mode";
        static readonly string description = "Task of the program Windows Auto Dark Mode.";

        public static bool CreateLogonTask()
        {
            try
            {
                using TaskService taskService = new();
                taskService.RootFolder.CreateFolder(folder, null, false);

                TaskDefinition tdLogon = taskService.NewTask();

                tdLogon.RegistrationInfo.Description = "Switches theme at user logon, replaces old autostart entry. " + description;
                tdLogon.RegistrationInfo.Author = author;
                tdLogon.RegistrationInfo.Source = program;
                tdLogon.Settings.DisallowStartIfOnBatteries = false;
                tdLogon.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                tdLogon.Settings.AllowHardTerminate = false;
                tdLogon.Settings.StartWhenAvailable = true;
                tdLogon.Settings.StopIfGoingOnBatteries = false;
                tdLogon.Settings.IdleSettings.StopOnIdleEnd = false;

                tdLogon.Triggers.Add(new LogonTrigger { UserId = Environment.UserDomainName + @"\" + Environment.UserName });
                tdLogon.Actions.Add(new ExecAction(Helper.ExecutionPath));

                taskService.GetFolder(folder).RegisterTaskDefinition(logon, tdLogon);
                Logger.Info("created logon task");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed to create logon task, ");
            }
            return false;
        }

        public static bool RemoveLogonTask()
        {
            using TaskService taskService = new();
            TaskFolder taskFolder = taskService.GetFolder(folder);
            if (taskFolder == null)
            {
                Logger.Debug("logon task does not exist (no taskFolder)");
                return true;
            }
            try
            {
                taskFolder.DeleteTask(logon, false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed removing logon task, ");
            }
            return false;
        }

        public static Task GetLogonTask()
        {
            using TaskService taskService = new();
            TaskFolder taskFolder = taskService.GetFolder(folder);
            if (taskFolder == null)
            {
                Logger.Info("logon task folder does not exist");
            }
            Task logonTask = taskService.GetTask(Path.Combine(folder, logon));
            return logonTask;
        }
    }
}
