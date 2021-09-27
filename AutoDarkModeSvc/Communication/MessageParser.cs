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
        private static readonly ComponentManager cm = ComponentManager.Instance();

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
                    case Command.Switch:
                        Logger.Info("signal received: invoke theme switch");
                        //cm.ForceAll();
                        ThemeManager.TimedSwitch(builder, false);
                        SendResponse(StatusCode.Ok);
                        break;

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
                        SendResponse(StatusCode.Ok);
                        break;

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
                            SendResponse(StatusCode.Ok);
                        }
                        else
                        {
                            SendResponse(StatusCode.Err);
                        }
                        break;

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
                            SendResponse(StatusCode.Ok);
                        }
                        else
                        {
                            SendResponse(StatusCode.Err);
                        }
                        break;

                    case Command.LocationAccess:
                        Logger.Info("signal received: checking location access permissions");
                        Task<bool> geoTask = Task.Run(async () => await LocationHandler.HasPermission());
                        geoTask.Wait();
                        var result = geoTask.Result;
                        if (result)
                        {
                            SendResponse(StatusCode.Ok);
                        }
                        else
                        {
                            SendResponse(StatusCode.NoLocAccess);
                        }
                        break;

                    case Command.CheckForUpdate:
                        Logger.Info("signal received: checking for update");
                        SendResponse(UpdateHandler.CheckNewVersion());
                        break;

                    case Command.Update:
                        Logger.Info("signal received: update adm");
                        //_ = UpdateHandler.CheckNewVersion();
                        ApiResponse response = UpdateHandler.CanAutoInstall();
                        if (response.StatusCode == StatusCode.New)
                        {
                            SendResponse(response.ToString());
                            // this is run sync, as such it will block the ZMQ thread!
                            UpdateHandler.Update();
                        }
                        else
                        {
                            SendResponse(response.ToString());
                        }
                        break;

                    case Command.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(StatusCode.Ok);
                        service.Exit(null, null);
                        break;

                    case Command.TestError:
                        Logger.Info("signal received: test error");
                        SendResponse(StatusCode.Err);
                        break;

                    case Command.Alive:
                        Logger.Info("signal received: request for running status");
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.Light:
                        Logger.Info("signal received: force light theme");
                        state.ForcedTheme = Theme.Light;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Light);
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.Dark:
                        Logger.Info("signal received: force dark theme");
                        state.ForcedTheme = Theme.Dark;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Dark);
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.NoForce:
                        Logger.Info("signal received: resetting forced modes");
                        state.ForcedTheme = Theme.Undefined;
                        ThemeManager.TimedSwitch(builder);
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.DetectMonitors:
                        Logger.Info("signal received: detecting new monitors");
                        WallpaperHandler.DetectMonitors();
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.CleanMonitors:
                        Logger.Info("signal received: removing disconnected monitors");
                        WallpaperHandler.CleanUpMonitors();
                        SendResponse(StatusCode.Ok);
                        break;

                    case Command.UpdateFailed:
                        Logger.Info("signal received: notify about failed update");
                        UpdateHandler.NotifyFailedUpdate();
                        SendResponse(StatusCode.Ok);
                        break;

                    default:
                        Logger.Debug("unknown message received");
                        SendResponse(StatusCode.Err);
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
