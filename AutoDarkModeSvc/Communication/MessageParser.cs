using AutoDarkModeConfig;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            WaitForConfigUpdateCompletion();
            msg.ForEach(message =>
            {
                switch (message)
                {
                    #region Switch
                    case Command.Switch:
                        Logger.Info("signal received: invoke theme switch");
                        //cm.ForceAll();
                        ThemeManager.TimedSwitch(builder, false);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Swap
                    case Command.Swap:
                        Logger.Info("signal received: swap themes");
                        if (RegistryHandler.AppsUseLightTheme())
                        {

                            ThemeManager.SwitchTheme(builder.Config, Theme.Dark);
                        }
                        else
                        {
                            ThemeManager.SwitchTheme(builder.Config, Theme.Light);
                        }
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region AddAutoStart
                    case Command.AddAutostart:
                        Logger.Info("signal received: add service to autostart");
                        bool regOk;
                        bool taskOk;
                        if (builder.Config.Tunable.UseLogonTask)
                        {
                            Logger.Debug("logon task mode selected");
                            regOk = RegistryHandler.RemoveAutoStart();
                            taskOk = TaskSchdHandler.CreateLogonTask();
                        }
                        else
                        {
                            Logger.Debug("autostart mode selected");
                            taskOk = TaskSchdHandler.RemoveLogonTask();
                            regOk = RegistryHandler.AddAutoStart();
                        }
                        if (regOk && taskOk)
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
                                StatusCode = StatusCode.Err,
                                Message = $"RegOk: {regOk}, TaskOk: {taskOk}"
                            }.ToString());
                        }
                        break;
                    #endregion

                    #region RemoveAutostart
                    case Command.RemoveAutostart:
                        Logger.Info("signal received: remove service from autostart");
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
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Ok
                            }.ToString());
                        }
                        else
                        {
                            SendResponse(new ApiResponse()
                            {
                                StatusCode = StatusCode.Err
                            }.ToString());
                        }
                        break;
                    #endregion

                    #region LocationAccess
                    case Command.LocationAccess:
                        Logger.Info("signal received: checking location access permissions");
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
                        Logger.Info("signal received: check if geolocator is busy");
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

                    #region Shutdown
                    case Command.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        service.Exit(null, null);
                        break;
                    #endregion

                    #region TestError
                    case Command.TestError:
                        Logger.Info("signal received: test error");
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
                        Logger.Info("signal received: force light theme");
                        state.ForcedTheme = Theme.Light;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Light);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region Dark
                    case Command.Dark:
                        Logger.Info("signal received: force dark theme");
                        state.ForcedTheme = Theme.Dark;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Dark);
                        SendResponse(StatusCode.Ok);
                        break;
                    #endregion

                    #region NoForce
                    case Command.NoForce:
                        Logger.Info("signal received: resetting forced modes");
                        state.ForcedTheme = Theme.Unknown;
                        ThemeManager.TimedSwitch(builder);
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region DetectMonitors
                    case Command.DetectMonitors:
                        Logger.Info("signal received: detecting new monitors");
                        WallpaperHandler.DetectMonitors();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
                        }.ToString());
                        break;
                    #endregion

                    #region CleanMonitors
                    case Command.CleanMonitors:
                        Logger.Info("signal received: removing disconnected monitors");
                        WallpaperHandler.CleanUpMonitors();
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

                    #region TestNotifications
                    case Command.TestNotifications:
                        Logger.Info("signal received: test notifications");
                        ToastHandler.InvokeUpdateInProgressToast();
                        SendResponse(new ApiResponse()
                        {
                            StatusCode = StatusCode.Ok
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

        private static void WaitForConfigUpdateCompletion()
        {
            bool notified = false;
            int retries = 5;
            for (int i = 0; i < retries; i++)
            {
                if (state.ConfigIsUpdating)
                {
                    if (notified)
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
    }
}
