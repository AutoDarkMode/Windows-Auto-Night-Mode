using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
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
            Logger.Info("starting command pipe server");
            while (AcceptConnections)
            {
                using NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName + Tools.DefaultPipeCommand, PipeDirection.In);
                pipeServer.WaitForConnection();
                using StreamReader sr = new StreamReader(pipeServer);
                List<string> msg = new List<string>();
                string temp;
                while ((temp = sr.ReadLine()) != null)
                {
                    msg.Add(temp);
                }
                Logger.Debug("client connection received with command: " + string.Join(",", msg));
                MsgParser(msg);
            }

            Running = false;
        }

        public void StopServer()
        {
            AcceptConnections = false;
            while (Running)
            {
                using NamedPipeClientStream npcs = new NamedPipeClientStream(".", PipeName + Tools.DefaultPipeCommand, PipeDirection.Out);
                try
                {
                    npcs.Connect(100);
                }
                catch (Exception)
                {
                    Logger.Warn("pipe server shutdown signal failed, retrying...");
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
                        TaskSchdHandler.RemoveTask();
                        SendResponse(Tools.Ok);
                        break;
                    case Tools.UpdateConfig:
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
                        SendResponse(Tools.Err);
                        break;
                }
            });
        }

        private void SendResponse(string message)
        {
            bool canClose = false;
            Task.Run(() =>
            {
                using NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName + Tools.DefaultPipeResponse, PipeDirection.Out);
                Logger.Debug("starting response pipe server after receiving message");
                pipeServer.WaitForConnection();
                canClose = true;
                using StreamWriter sw = new StreamWriter(pipeServer)
                {
                    AutoFlush = true
                };
                sw.WriteLine(message);
                Logger.Debug("response request served, closing response pipe server");
            });
            // cheesy way to end WaitForConnection() if called synchronously
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                while (!canClose)
                {
                    try
                    {
                        using NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName + Tools.DefaultPipeResponse, PipeDirection.In);
                        pipeClient.Connect(1000);
                        Logger.Debug("stopping open response pipe server after exceeding timout grace period");
                        using StreamReader sr = new StreamReader(pipeClient);
                        // Display the read text to the console
                        string temp;
                        while ((temp = sr.ReadLine()) != null) { }
                        canClose = true;
                    }
                    catch (TimeoutException)
                    {
                        Logger.Debug("reponse pipe already closed");
                        canClose = true;
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e, "automatic response pipe disconnection reported an error");
                        canClose = true;
                    }
                }
            });
        }
    }
}