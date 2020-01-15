using AutoDarkMode;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using System;
using System.Collections.Generic;
using System.Text;

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

            AutoDarkModeConfigBuilder Properties = AutoDarkModeConfigBuilder.Instance();
            try
            {
                Properties.Load();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "could not read config file");
                return;
            }

            msg.ForEach(message =>
            {
                switch (message)
                {
                    case PipeMessage.Switch:
                        Logger.Info("signal received: time based theme switch");
                        ThemeManager.TimedSwitch(Properties.Config);
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.Swap:
                        Logger.Info("signal received: swap themes");
                        if (RegistryHandler.AppsUseLightTheme())
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        }
                        else
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        }
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.Dark:
                        Logger.Info("signal received: switch to dark mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.Light:
                        Logger.Info("signal received: switch to light mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.AddAutostart:
                        Logger.Info("signal received: adding service to autostart");
                        RegistryHandler.AddAutoStart();
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.RemoveAutostart:
                        Logger.Info("signal received: removing service from autostart");
                        RegistryHandler.RemoveAutoStart();
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.CreateTask:
                        Logger.Info("signal received: creating win scheduler based time switch task");
                        try
                        {
                            DateTime sunrise = Convert.ToDateTime(Properties.Config.Sunrise);
                            DateTime sunset = Convert.ToDateTime(Properties.Config.Sunset);
                            if (!Properties.Config.Location.Disabled)
                            {
                                LocationHandler.ApplySunDateOffset(Properties.Config, out sunrise, out sunset);
                            }
                            TaskSchdHandler.CreateSwitchTask(sunrise.Hour, sunrise.Minute, sunset.Hour, sunset.Minute);
                            SendResponse(PipeMessage.Ok);
                        }
                        catch (FormatException e)
                        {
                            Logger.Error(e, "could not create win scheduler tasks");
                            SendResponse(PipeMessage.Err);
                            Console.WriteLine(e);
                        }
                        break;
                    case PipeMessage.RemoveTask:
                        Logger.Info("signal received: removing win tasks");
                        TaskSchdHandler.RemoveTask();
                        SendResponse(PipeMessage.Ok);
                        break;
                    case PipeMessage.UpdateConfig:
                        Logger.Info("signal received: updating configuration file");
                        try
                        {
                            AutoDarkModeConfigBuilder.Instance().Load();
                            SendResponse(PipeMessage.Ok);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "could not read config file");
                            SendResponse(PipeMessage.Err);
                        }
                        break;
                    case PipeMessage.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(PipeMessage.Ok);
                        service.Exit(null, null);
                        break;
                    case PipeMessage.TestError:
                        Logger.Info("signal received: test error");
                        SendResponse(PipeMessage.Err);
                        break;
                }
            });
        }
    }
}
