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
        public static void Parse(List<string> msg, Action<string> SendResponse)
        {

            AutoDarkModeConfigBuilder Properties = AutoDarkModeConfigBuilder.Instance();
            try
            {
                Properties.Read();
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
                    case Tools.Switch:
                        Logger.Info("signal received: time based theme switch");
                        ThemeManager.TimedSwitch(Properties.Config);
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.Swap:
                        Logger.Info("signal received: swap themes");
                        if (RegistryHandler.AppsUseLightTheme())
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        }
                        else
                        {
                            ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        }
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.Dark:
                        Logger.Info("signal received: switch to dark mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.Light:
                        Logger.Info("signal received: switch to light mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.AddAutostart:
                        Logger.Info("signal received: adding service to autostart");
                        RegistryHandler.AddAutoStart();
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.RemoveAutostart:
                        Logger.Info("signal received: removing service from autostart");
                        RegistryHandler.RemoveAutoStart();
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.CreateTask:
                        Logger.Info("signal received: creating win scheduler based time switch task");
                        try
                        {
                            DateTime sunrise = Convert.ToDateTime(Properties.Config.Sunrise);
                            DateTime sunset = Convert.ToDateTime(Properties.Config.Sunset);
                            if (!Properties.Config.Location.Disabled)
                            {
                                ThemeManager.CalculateSunTimes(Properties.Config, out sunrise, out sunset);
                            }
                            TaskSchdHandler.CreateTask(sunrise.Hour, sunrise.Minute, sunset.Hour, sunset.Minute);
                            SendResponse(Tools.Ok);
                        }
                        catch (FormatException e)
                        {
                            Logger.Error(e, "could not create win scheduler tasks");
                            SendResponse(Tools.Err);
                            Console.WriteLine(e);
                        }
                        break;
                    case Tools.RemoveTask:
                        Logger.Info("signal received: removing win tasks");
                        TaskSchdHandler.RemoveTask();
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.UpdateConfig:
                        Logger.Info("signal received: updating configuration file");
                        try
                        {
                            AutoDarkModeConfigBuilder.Instance().Read();
                            SendResponse(Tools.Ok);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "could not read config file");
                            SendResponse(Tools.Err);
                        }
                        break;
                    case Tools.TestError:
                        Logger.Info("signal received: test error");
                        SendResponse(Tools.Err);
                        break;
                }
            });
        }
    }
}
