using AutoDarkMode;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
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
                    case Command.Switch:
                        Logger.Info("signal received: time based theme switch");
                        ThemeManager.TimedSwitch(Properties.Config);
                        SendResponse(Command.Ok);
                        break;

                    case Command.Swap:
                        Logger.Info("signal received: swap themes");
                        if (RegistryHandler.AppsUseLightTheme())
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        }
                        else
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        }
                        SendResponse(Command.Ok);
                        break;

                    case Command.Dark:
                        Logger.Info("signal received: switch to dark mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        SendResponse(Command.Ok);
                        break;

                    case Command.Light:
                        Logger.Info("signal received: switch to light mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        SendResponse(Command.Ok);
                        break;

                    case Command.AddAutostart:
                        Logger.Info("signal received: adding service to autostart");
                        RegistryHandler.AddAutoStart();
                        SendResponse(Command.Ok);
                        break;

                    case Command.RemoveAutostart:
                        Logger.Info("signal received: removing service from autostart");
                        RegistryHandler.RemoveAutoStart();
                        SendResponse(Command.Ok);
                        break;

                    case Command.CreateTask:
                        Logger.Info("signal received: creating win scheduler based time switch task");
                        try
                        {
                            DateTime sunrise = Convert.ToDateTime(Properties.Config.Sunrise);
                            DateTime sunset = Convert.ToDateTime(Properties.Config.Sunset);
                            if (Properties.Config.Location.Enabled)
                            {
                                LocationHandler.ApplySunDateOffset(Properties.Config, out sunrise, out sunset);
                            }
                            TaskSchdHandler.CreateSwitchTask(sunrise.Hour, sunrise.Minute, sunset.Hour, sunset.Minute);
                            SendResponse(Command.Ok);
                        }
                        catch (FormatException e)
                        {
                            Logger.Error(e, "could not create win scheduler tasks");
                            SendResponse(Command.Err);
                            Console.WriteLine(e);
                        }
                        break;
                    case Command.RemoveTask:

                        Logger.Info("signal received: removing win tasks");
                        TaskSchdHandler.RemoveTasks();
                        SendResponse(Command.Ok);
                        break;

                    case Command.Location:
                        Logger.Info("signal received: request location update");
                        Task<bool> geoTask = Task.Run(() => LocationHandler.UpdateGeoposition(AutoDarkModeConfigBuilder.Instance()));
                        geoTask.Wait();
                        var result = geoTask.Result;
                        if (result)
                        {
                            SendResponse(Command.Ok);
                        }
                        else
                        {
                            SendResponse(Command.NoLocAccess);
                        }
                        break;

                    case Command.UpdateConfig:
                        Logger.Info("signal received: updating configuration file");
                        try
                        {
                            AutoDarkModeConfigBuilder.Instance().Load();
                            SendResponse(Command.Ok);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "could not read config file");
                            SendResponse(Command.Err);
                        }
                        break;

                    case Command.Update:
                        Logger.Info("signal received: checking for update");
                        SendResponse(UpdateHandler.CheckNewVersion());
                        break;

                    case Command.Shutdown:
                        Logger.Info("signal received, exiting");
                        SendResponse(Command.Ok);
                        service.Exit(null, null);
                        break;
                    case Command.TestError:
                        Logger.Info("signal received: test error");
                        SendResponse(Command.Err);
                        break;
                    case Command.Alive:
                        Logger.Info("signal received: am I alive?");
                        SendResponse(Command.Ok);
                        break;
                    default:
                        Logger.Debug("unknown message received");
                        SendResponse(Command.Err);
                        break;
                }
            });
        }
    }
}
