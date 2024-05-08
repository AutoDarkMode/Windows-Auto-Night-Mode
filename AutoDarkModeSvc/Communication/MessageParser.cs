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
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers.IThemeManager2;

namespace AutoDarkModeSvc.Communication
{
    static class MessageParser
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private static readonly GlobalState state = GlobalState.Instance();
        //private static readonly ComponentManager cm = ComponentManager.Instance();

        /// <summary>
        /// Parses a command message and invokes a callback function delegate for status reporting
        /// </summary>
        /// <param name="msg">list of messages to parse</param>
        /// <param name="SendResponse">Callback taking a string as parameter to report return values back to sender</param>
        /// <param name="service">Service class for invoking application exit</param>
        public static void Parse(List<string> msg, Action<string> SendResponse, Service service)
        {
            //WaitForConfigUpdateCompletion();
            _ = state.ConfigIsUpdatingWaitHandle.WaitOne();
            msg.ForEach(message =>
            {
                switch (message)
                {
                    #region Switch
                    case Command.RequestSwitch:
                        Logger.Info("signal received: invoke theme switch request");
                        //cm.ForceAll();
                        ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Swap
                    case Command.Swap:
                        Logger.Info("signal received: swap themes");
                        Theme theme = ThemeManager.SwitchThemeAutoPause(source: SwitchSource.Api);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = Enum.GetName(typeof(Theme), theme)
                        }.ToString());
                        break;
                    #endregion

                    #region AddAutoStart
                    case Command.AddAutostart:
                        Logger.Info("signal received: add service to autostart");
                        SendResponse(AutoStartHandler.AddAutostart().ToString());
                        break;
                    #endregion

                    #region RemoveAutostart
                    case Command.RemoveAutostart:
                        Logger.Info("signal received: remove service from autostart");
                        SendResponse(AutoStartHandler.RemoveAutostart().ToString());
                        break;
                    #endregion

                    #region GetAutostartState
                    case Command.GetAutostartState:
                        Logger.Info("signal recevied: get autostart state");
                        SendResponse(AutoStartHandler.GetAutostartState().ToString());
                        break;
                    #endregion

                    #region ValidateAutostartAutostart
                    case string s when s.StartsWith(Command.ValidateAutostart):
                        Logger.Info("signal received: validate autostart entries");
                        string[] split = s.Split(" ");
                        if (split.Length > 1)
                        {
                            SendResponse(AutoStartHandler.Validate(true).ToString());
                        }
                        else
                        {
                            SendResponse(AutoStartHandler.Validate(false).ToString());
                        }
                        break;
                    #endregion

                    #region LocationAccess
                    case Command.LocationAccess:
                        Logger.Debug("signal received: checking location access permissions");
                        Task<bool> geoTask = Task.Run(async () => await LocationHandler.HasLocation());
                        geoTask.Wait();
                        var result = geoTask.Result;
                        if (result)
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Ok
                            }.ToString());
                        }
                        else
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.NoLocAccess,
                                Message = "location service needs to be enabled"
                            }.ToString());
                        }
                        break;
                    #endregion

                    #region GeoloatorIsUpdating
                    case Command.GeolocatorIsUpdating:
                        Logger.Debug("signal received: check if geolocator is busy");
                        if (state.GeolocatorIsUpdating)
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.InProgress
                            }.ToString());
                        }
                        else
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Ok
                            }.ToString());
                        }
                        break;
                    #endregion

                    #region CheckForUpdates
                    case Command.CheckForUpdate:
                        Logger.Info("signal received: checking for update");
                        SendResponse(UpdateHandler.CheckNewVersion().ToString());
                        break;
                    #endregion

                    #region CheckForUpdateNotify
                    case Command.CheckForUpdateNotify:
                        Logger.Info("signal received: checking for update and requesting notification");
                        ApiResponse updateCheckData = UpdateHandler.CheckNewVersion();
                        updateCheckData = UpdateHandler.CanUseUpdater();
                        if (updateCheckData.StatusCode == StatusCode.New)
                        {
                            ToastHandler.InvokeUpdateToast();
                        }
                        else if (updateCheckData.StatusCode == StatusCode.UnsupportedOperation || updateCheckData.StatusCode == StatusCode.Disabled)
                        {
                            ToastHandler.InvokeUpdateToast(canUseUpdater: false);
                        }
                        SendResponse(updateCheckData.ToString());
                        break;
                    #endregion

                    #region Update
                    case Command.Update:
                        Logger.Info("signal received: update adm");
                        if (!UpdateHandler.Updating)
                        {
                            ApiResponse response = UpdateHandler.CanUseUpdater();
                            if (response.StatusCode == StatusCode.New)
                            {
                                SendResponse(response.ToString());
                                // this is run sync, as such it will block the ZMQ thread!
                                _ = Task.Run(() => UpdateHandler.Update());
                            }
                            else
                            {
                                SendResponse(response.ToString());
                            }
                        }
                        else
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.InProgress,
                                Message = "Update already in progress",
                                Details = $"Download Progress: {UpdateHandler.Progress}"
                            }.ToString());
                        }
                        //_ = UpdateHandler.CheckNewVersion();

                        break;
                    #endregion

                    #region CheckForDowngradeNotify
                    case Command.CheckForDowngradeNotify:
                        Logger.Info("signal received: checking for downgrade and requesting notification");
                        ApiResponse downgradeCheckData = UpdateHandler.CheckDowngrade();
                        updateCheckData = UpdateHandler.CanUseUpdater();
                        if (updateCheckData.StatusCode == StatusCode.Downgrade)
                        {
                            ToastHandler.InvokeUpdateToast(downgrade: true);
                        }
                        else if (updateCheckData.StatusCode == StatusCode.UnsupportedOperation || updateCheckData.StatusCode == StatusCode.Disabled)
                        {
                            ToastHandler.InvokeUpdateToast(downgrade: true, canUseUpdater: false);
                        }
                        SendResponse(updateCheckData.ToString());
                        break;
                    #endregion

                    #region Shutdown
                    case Command.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        service.RequestExit(null, null);
                        break;
                    #endregion

                    #region Restart
                    case Command.Restart:
                        Logger.Info("signal received, restarting");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        service.Restart(null, new ExitEventArgs(false));
                        break;
                    #endregion

                    #region TestError
                    case Command.TestError:
                        Logger.Info("signal received: test error");
                        Thread.Sleep(5000);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Err
                        }.ToString());
                        break;
                    #endregion

                    #region Alive
                    case Command.Alive:
                        Logger.Info("signal received: request for running status");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Light
                    case Command.Light:
                        Logger.Info("signal received: set light theme");
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                        ThemeManager.SwitchThemeAutoPause(target: Theme.Light, source: SwitchSource.Api);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Dark
                    case Command.Dark:
                        Logger.Info("signal received: set dark theme");
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Dark);
                        ThemeManager.SwitchThemeAutoPause(target: Theme.Dark, source: SwitchSource.Api);
                        SendResponse(StatusCode.Ok);
                        break;
                    #endregion

                    #region ForceLight
                    case Command.ForceLight:
                        Logger.Info("signal received: force light theme");
                        state.ForcedTheme = Theme.Light;
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                        ThemeManager.UpdateTheme(new(SwitchSource.Api, Theme.Light));
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region ForceDark
                    case Command.ForceDark:
                        Logger.Info("signal received: force dark theme");
                        state.ForcedTheme = Theme.Dark;
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Dark);
                        ThemeManager.UpdateTheme(new(SwitchSource.Api, Theme.Dark));
                        SendResponse(StatusCode.Ok);
                        break;
                    #endregion

                    #region NoForce
                    case Command.NoForce:
                        Logger.Info("signal received: resetting forced modes");
                        state.ForcedTheme = Theme.Unknown;
                        ThemeManager.RequestSwitch(new(SwitchSource.Api));
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region ToggleSkipNext
                    case Command.ToggleSkipNext:
                        Logger.Info("signal received: toggle skip next switch");
                        bool enabled = state.PostponeManager.ToggleSkipNextSwitch();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = enabled.ToString()
                        }.ToString());
                        break;
                    #endregion

                    #region GetPostponeStatus
                    case Command.GetPostponeStatus:
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = state.PostponeManager.IsPostponed.ToString(),
                            Details = state.PostponeManager.MakeQueueDto().Serialize()
                        }.ToString());
                        break;
                    #endregion

                    #region ClearPostponeQueue
                    case Command.ClearPostponeQueue:
                        state.PostponeManager.RemoveUserClearablePostpones();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region DelayBy
                    case string s when s.StartsWith(Command.DelayBy):
                        string minutesString = message.Replace(Command.DelayBy, "").Trim();
                        string statusCode = StatusCode.Err;
                        if (int.TryParse(minutesString, out int minutes))
                        {
                            Logger.Info($"signal received: delay theme switch by {minutesString} minutes");
                            state.PostponeManager.Add(new(Helper.PostponeItemDelayAutoSwitch, DateTime.Now.AddMinutes(minutes), SkipType.Unspecified));
                            statusCode = StatusCode.Ok;
                        }
                        else
                        {
                            Logger.Info($"signal received: delay theme switch with invalid data ({minutesString})");
                        }
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = statusCode
                        }.ToString());
                        break;
                    #endregion

                    #region RequestedTheme
                    case Command.GetRequestedTheme:
                        Logger.Info("signal received: get requested theme");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = Enum.GetName(typeof(Theme), state.InternalTheme)
                        }.ToString());
                        break;
                    #endregion

                    #region CurrentColorization
                    case Command.GetCurrentColorization:
                        Logger.Info("signal received: current colorization");
                        try
                        {
                            string colCol = RegistryHandler.GetAccentColor();
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Ok,
                                Message = colCol.Replace("0x", "#"),
                                Details = Enum.GetName(state.InternalTheme)
                            }.ToString());
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "error while retrieving colorizatoin color:");
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Err,
                                Message = ex.Message,
                                Details = ex.Source
                            }.ToString());
                        }                        
                        break;
                    #endregion

                    #region DetectMonitors
                    case Command.DetectMonitors:
                        Logger.Info("signal received: detecting new monitors");
                        DisplayHandler.DetectMonitors();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region CleanMonitors
                    case Command.CleanMonitors:
                        Logger.Info("signal received: removing disconnected monitors");
                        DisplayHandler.CleanUpMonitors();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region UpdateFailed
                    case Command.UpdateFailed:
                        Logger.Info("signal received: notify about failed update");
                        ToastHandler.InvokeFailedUpdateToast();
                        SendResponse(StatusCode.Ok);
                        break;
                    #endregion

                    #region GetLearnedThemeNames
                    case Command.GetLearnedThemeNames:
                        Logger.Debug("signal received: get learned theme names");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = Helper.SerializeLearnedThemesDict(state.LearnedThemeNames)
                        }.ToString());
                        break;
                    #endregion

                    #region TestNotifications
                    case Command.TestNotifications:
                        Logger.Info("signal received: test notifications");
                        ToastHandler.InvokeUpdateInProgressToast("TestVersion");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region TestNotifications2
                    case Command.TestNotifications2:
                        Logger.Info("signal received: test notifications");
                        //ToastHandler.InvokeUpdateToast(true, true);
                        //ToastHandler.RemoveUpdaterToast();
                        //ToastHandler.UpdateProgressToast("0.5", "test");
                        //ToastHandler.InvokeUpdateToast();
                        ToastHandler.InvokeFailedUpdateToast();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Test
                    case Command.Test:
                        PostponeManager pm = state.PostponeManager;
                        state.PostponeManager.Add(new("testpostpone", DateTime.Now.AddMinutes(1), SkipType.Unspecified));
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            Message = "it works"
                        }.ToString());
                        break;
                    #endregion

                    #region Test2
                    case Command.Test2:
                        //ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                        //Cursors current = RegistryHandler.GetCursors();
                        //Cursors byName = RegistryHandler.GetCursorScheme("Posy's Cursor");
                        // state.PostponeManager.SyncExpiryTimesWithSystemClock();
                        WallpaperHandler.AdvanceSlideshow(WallpaperHandler.DesktopSlideshowDirection.Forward);
                        //UpdateHandler.EndBlockingProcesses(out bool shellRestart, out bool appRestart);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok,
                            //Message = $"shellRestart: {shellRestart}, appRestart: {appRestart}"
                        }.ToString());
                        break;
                    #endregion

                    default:
                        Logger.Debug("unknown message received");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Err,
                            Message = "requested command does not exist"
                        }.ToString());
                        break;
                }
            });
        }

        /*
        private static void WaitForConfigUpdateCompletion()
        {
            bool notified = false;
            int retries = 100;
            for (int i = 0; i < retries; i++)
            {
                if (state.ConfigIsUpdating)
                {
                    if (!notified)
                    {
                        Logger.Debug("waiting for config update to finish");
                        notified = true;
                    }
                    Thread.Sleep(100);
                }
                else
                {
                    break;
                }
            }
        }
        */
    }
}
