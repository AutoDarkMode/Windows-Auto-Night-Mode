using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;

namespace AutoDarkModeSvc.Communication
{
    class PipeServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string PipeName { get; set; }
        public bool Running { get; private set; }
        private bool AcceptConnections;


        public PipeServer(string pipename)
        {
            PipeName = pipename;
            Running = false;
            AcceptConnections = false;
        }

        public void StartServer()
        {
            Running = true;
            AcceptConnections = true;
            Logger.Info("starting pipe server");
            while (AcceptConnections)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
                {
                    pipeServer.WaitForConnection();
                    Logger.Debug("client connection received");
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        List<string> msg = new List<string>();
                        string temp;
                        while ((temp = sr.ReadLine()) != null)
                        {
                            msg.Add(temp);
                        }
                        MsgParser(msg);
                    }
                }
            }

            Running = false;
        }

        public void StopServer()
        {
            AcceptConnections = false;
            while (Running)
            {
                using (NamedPipeClientStream npcs = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {

                    try
                    {
                        npcs.Connect(100);
                    }
                    catch (Exception)
                    {
                       Logger.Warn("pipe server shutdown signal failed, retrying...");
                    }
                }
            }
            Logger.Info("pipe server shutdown confirmed");
        }

        public void MsgParser(List<string> msg)
        {
            AutoDarkModeConfigBuilder Properties = AutoDarkModeConfigBuilder.Instance();
            try
            {
                Properties.Read();
            } catch (Exception ex)
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
                        break;
                    case Tools.Dark:
                        Logger.Info("signal received: switch to dark mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Dark);
                        break;
                    case Tools.Light:
                        Logger.Info("signal received: switch to light mode");
                        ThemeManager.SwitchTheme(Properties.Config, Theme.Light);
                        break;
                    case Tools.AddAutostart:
                        Logger.Info("signal received: adding service to autostart");
                        RegistryHandler.AddAutoStart();
                        break;
                    case Tools.RemoveAutostart:
                        Logger.Info("signal received: removing service from autostart");
                        RegistryHandler.RemoveAutoStart();
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

                        }
                        catch (FormatException e)
                        {
                            Logger.Error(e, "could not create win scheduler tasks");
                            Console.WriteLine(e);
                        }
                        break;
                    case Tools.RemoveTask:
                        TaskSchdHandler.RemoveTask();
                        break;
                    case Tools.UpdateConfig:
                        try
                        {
                            AutoDarkModeConfigBuilder.Instance().Read();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "could not read config file");
                        }
                        break;
                }
            });
        }
    }
}