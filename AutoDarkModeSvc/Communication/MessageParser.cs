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

        /// <summary>
        /// Parses a command message and invokes a callback function delegate for status reporting
        /// </summary>
        /// <param name="msg">list of messages to parse</param>
        /// <param name="SendResponse">Callback taking a string as parameter to report return values back to sender</param>
        /// <param name="service">Service class for invoking application exit</param>
        public static void Parse(List<string> msg, Action<string> SendResponse, Service service)
        {

            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            GlobalState state = GlobalState.Instance();
            ComponentManager cm = ComponentManager.Instance();
            msg.ForEach(message =>
            {
                switch (message)
                {
                    case Command.Switch:
                        Logger.Info("signal received: invoke theme switch");
                        int retries = 3;
                        for (int i = 0; i < retries; i++)
                        {
                            if (builder.Loading)
                            {
                                Thread.Sleep(100);
                            }
                            else
                            {
                                break;
                            }
                        }
                        //cm.ForceAll();
                        ThemeManager.TimedSwitch(builder, false);
                        SendResponse(Response.Ok);
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
                        SendResponse(Response.Ok);
                        break;

                    case Command.AddAutostart:
                        Logger.Info("signal received: adding service to autostart");
                        bool regOk;
                        bool taskOk;
                        if (builder.Config.Tunable.UseLogonTask)
                        {
                            regOk = RegistryHandler.RemoveAutoStart();
                            taskOk = TaskSchdHandler.CreateLogonTask();
                        }
                        else
                        {
                            taskOk = TaskSchdHandler.RemoveLogonTask();
                            regOk = RegistryHandler.AddAutoStart();
                        }
                        if (regOk && taskOk)
                        {
                            SendResponse(Response.Ok);
                        }
                        else
                        {
                            SendResponse(Response.Err);
                        }
                        break;

                    case Command.RemoveAutostart:
                        Logger.Info("signal received: removing service from autostart");
                        bool ok;
                        if (builder.Config.Tunable.UseLogonTask)
                        {
                            ok = TaskSchdHandler.RemoveLogonTask();
                        }
                        else
                        {
                            ok = RegistryHandler.RemoveAutoStart();
                        }
                        if (ok)
                        {
                            SendResponse(Response.Ok);
                        }
                        else
                        {
                            SendResponse(Response.Err);
                        }
                        break;

                    case Command.Location:
                        Logger.Info("signal received: request location update");
                        Task<bool> geoTask = Task.Run(() => LocationHandler.UpdateGeoposition(AdmConfigBuilder.Instance()));
                        geoTask.Wait();
                        var result = geoTask.Result;
                        if (result)
                        {
                            SendResponse(Response.Ok);
                        }
                        else
                        {
                            SendResponse(Response.NoLocAccess);
                        }
                        break;

                    case Command.UpdateConfig:
                        Logger.Info("signal received: updating configuration files");
                        try
                        {
                            AdmConfigBuilder.Instance().Load();
                            AdmConfigBuilder.Instance().LoadLocationData();
                            SendResponse(Response.Ok);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "could not read config file");
                            SendResponse(Response.Err);
                        }
                        break;

                    case Command.Update:
                        Logger.Info("signal received: checking for update");
                        SendResponse(UpdateHandler.CheckNewVersion());
                        break;

                    case Command.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(Response.Ok);
                        service.Exit(null, null);
                        break;

                    case Command.TestError:
                        Logger.Info("signal received: test error");
                        SendResponse(Response.Err);
                        break;

                    case Command.Alive:
                        Logger.Info("signal received: request for running status");
                        SendResponse(Response.Ok);
                        break;

                    case Command.Light:
                        Logger.Info("signal received: force light theme");
                        state.ForcedTheme = Theme.Light;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Light);
                        SendResponse(Response.Ok);
                        break;

                    case Command.Dark:
                        Logger.Info("signal received: force dark theme");
                        state.ForcedTheme = Theme.Dark;
                        ThemeManager.SwitchTheme(builder.Config, Theme.Dark);
                        SendResponse(Response.Ok);
                        break;

                    case Command.NoForce:
                        Logger.Info("signal received: resetting forced modes");
                        state.ForcedTheme = Theme.Undefined;
                        ThemeManager.TimedSwitch(builder);
                        SendResponse(Response.Ok);
                        break;

                    case Command.DetectMonitors:
                        Logger.Info("signal received: detecting new monitors");
                        WallpaperHandler.DetectMonitors();
                        SendResponse(Response.Ok);
                        break;

                    default:
                        Logger.Debug("unknown message received");
                        SendResponse(Response.Err);
                        break;
                }
            });
        }
    }
}
