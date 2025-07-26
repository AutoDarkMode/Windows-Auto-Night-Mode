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
using System.Linq;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeSvc.Handlers;

public static class AutoStartHandler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    public static ApiResponse AddAutostart(bool modified = false)
    {
        bool regOk = false;
        bool taskOk;
        if (builder.Config.Tunable.UseLogonTask)
        {
            Logger.Debug("logon task mode selected");
            taskOk = TaskSchdHandler.CreateLogonTask();
            if (taskOk) regOk = RegistryHandler.RemoveAutoStart();

            if (regOk & taskOk)
            {
                return new ApiResponse()
                {
                    StatusCode = modified ? StatusCode.Modified : StatusCode.Ok,
                    Message = "autostart entry set (logon task)",
                    Details = $"registry removal success: {regOk}"
                };
            }
            else
            {
                regOk = RegistryHandler.AddAutoStart();
                return new ApiResponse()
                {
                    StatusCode = StatusCode.Err,
                    Message = "failed setting logon task, trying to set autostart entry",
                    Details = $"autostart entry set success: {regOk}"
                };
            }
        }
        else
        {
            Logger.Debug("autostart mode selected");
            taskOk = TaskSchdHandler.RemoveLogonTask();
            regOk = RegistryHandler.AddAutoStart();
            if (regOk)
            {
                return new ApiResponse()
                {
                    StatusCode = modified ? StatusCode.Modified : StatusCode.Ok,
                    Message = "added autostart task successfully",
                    Details = $"task removal success: {taskOk}"
                };
            }
        }

        return new ApiResponse()
        {
            StatusCode = StatusCode.Err,
            Message = "autostart error",
            Details = $"regOk: {regOk}, taskOk: {taskOk}"
        };
    }

    public static ApiResponse RemoveAutostart()
    {
        bool ok;
        if (builder.Config.Tunable.UseLogonTask)
        {
            Logger.Debug("logon task mode selected");
            ok = TaskSchdHandler.RemoveLogonTask();
        }
        else
        {
            Logger.Debug("autostart mode selected");
            ok = RegistryHandler.RemoveAutoStart();
        }

        if (ok)
        {
            return new ApiResponse()
            {
                StatusCode = StatusCode.Ok
            };
        }
        else
        {
            return new ApiResponse()
            {
                StatusCode = StatusCode.Err
            };
        }
    }

    public static ApiResponse GetAutostartState()
    {
        if (builder.Config.Tunable.UseLogonTask)
        {
            try
            {
                using Microsoft.Win32.TaskScheduler.Task logonTask = TaskSchdHandler.GetLogonTask();
                if (logonTask != null)
                {
                    ApiResponse response = new()
                    {
                        StatusCode = StatusCode.AutostartTask,
                        Message = $"Enabled: {logonTask.Enabled}",
                        Details = logonTask.Definition.Actions.ToString()
                    };
                    return response;
                }
            }
            catch (Exception ex)
            {
                string msg = "error while getting logon task state:";
                Logger.Error(ex, msg);
                return new()
                {
                    StatusCode = StatusCode.Err,
                    Message = msg,
                    Details = $"Exception:\nMessage: {ex.Message}\nSource:{ex.Source}"
                };
            }
        }
        else
        {
            string autostartPath = RegistryHandler.GetAutostartPath();
            if (autostartPath != null)
            {
                bool approved = RegistryHandler.IsAutostartApproved();
                if (approved)
                {
                    return new()
                    {
                        StatusCode = StatusCode.AutostartRegistryEntry,
                        Message = "Enabled",
                        Details = autostartPath.Replace("\"", "")
                    };
                }
                else
                {
                    return new()
                    {
                        StatusCode = StatusCode.AutostartRegistryEntry,
                        Message = "Disabled",
                        Details = autostartPath.Replace("\"", "")
                    };
                }
            }
        }
        return new ApiResponse()
        {
            StatusCode = StatusCode.Disabled,
            Message = "no auto start entries found"
        };
    }

    /// <summary>
    /// Validates the Auto Dark Mode autostart entries for correctness
    /// </summary>
    /// <returns>true if the state is valid, false if the state was invalid</returns>
    public static ApiResponse Validate(bool alwaysValidate)
    {
        if (!builder.Config.Autostart.Validate && !alwaysValidate)
        {
            return new()
            {
                StatusCode = StatusCode.Ok,
                Message = "validation disabled"
            };
        }
        //check if a logon task if used, needs to be reset if the folder is empty or the path differs from the execution base directory
        if (builder.Config.Tunable.UseLogonTask)
        {
            try
            {
                string autostartPath = RegistryHandler.GetAutostartPath();
                using Microsoft.Win32.TaskScheduler.Task logonTask = TaskSchdHandler.GetLogonTask();
                if (logonTask == null)
                {
                    Logger.Warn("autostart validation failed, missing logon task folder. fixing autostart");
                    ApiResponse result = AddAutostart(modified: true);
                    result.Details += "\nvalidation mode: recreate task (missing folder)";
                    return result;
                }
                else if (logonTask.Definition.Actions.First().ToString().Trim() != Helper.ExecutionPath)
                {
                    Logger.Warn("autostart validation failed, wrong execution path. fixing autostart");
                    ApiResponse result = AddAutostart(modified: true);
                    result.Details += "\nvalidation mode: recreate task (wrong path)";
                    return result;
                }
                else if (autostartPath != null)
                {
                    Logger.Warn("autostart validation failed, wrong execution path. fixing autostart");
                    ApiResponse result = AddAutostart(modified: true);
                    result.Details += "\nvalidation mode: recreate task (regkey still active)";
                    return result;
                }
            }
            catch (Exception ex)
            {
                string msg = "error during autostart validation:";
                Logger.Error(ex, msg);
                return new()
                {
                    StatusCode = StatusCode.Err,
                    Message = msg,
                    Details = $"Exception:\nMessage: {ex.Message}\nSource:{ex.Source}"
                };
            }
        }
        //check if registry keys are used, needs to be reset if the key is missing or the path differs from the execution base directory
        else
        {
            bool recreate = false;
            string reason = "";
            try
            {
                using Microsoft.Win32.TaskScheduler.Task logonTask = TaskSchdHandler.GetLogonTask();
                if (logonTask != null)
                {
                    recreate = true;
                    reason = "logon task active in registry mode";
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not determine task scheduler autostart state:");
            }
            if (!recreate)
            {
                string autostartPath = RegistryHandler.GetAutostartPath();
                if (autostartPath == null)
                {
                    reason = "missing entry";
                    recreate = true;
                }
                else
                {
                    autostartPath = autostartPath.Replace("\"", "");
                    if (!autostartPath.Contains(Helper.ExecutionPath))
                    {
                        reason = "wrong path";
                        recreate = true;
                    }
                }
            }

            if (recreate)
            {
                Logger.Warn($"autostart validation failed ({reason}), recreate autostart registry keys");
                ApiResponse result = AddAutostart(modified: true);
                result.Details += $"\nvalidation mode: recreate regkey ({reason})";
                return result;
            }
        }
        return new()
        {
            StatusCode = StatusCode.Ok,
            Message = "autostart entries valid"
        };
    }
}
