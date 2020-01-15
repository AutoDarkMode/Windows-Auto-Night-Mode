using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using AutoDarkMode;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;

namespace AutoDarkModeSvc.Communication
{
    class PipeServer : ICommandServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string PipeName { get; set; }
        private Service Service { get; }
        private Task Task { get; set; }
        public bool Running { get; private set; }
        private bool AcceptConnections { get; set; }

        public PipeServer(string pipename, Service service)
        {
            PipeName = pipename;
            Running = false;
            AcceptConnections = false;
            Service = service;
        }

        public void Start()
        {
            Task = Task.Run(() =>
            {
                Running = true;
                AcceptConnections = true;
                Logger.Info("starting command pipe server");
                while (AcceptConnections)
                {
                    using NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName + PipeMessage.DefaultPipeCommand, PipeDirection.In);
                    pipeServer.WaitForConnection();
                    using StreamReader sr = new StreamReader(pipeServer);
                    List<string> msg = new List<string>();
                    string temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        msg.Add(temp);
                    }
                    Logger.Info("client connection received with command: " + string.Join(",", msg));
                    MessageParser.Parse(msg, SendResponse, Service);
                }

                Running = false;
            });            
        }

        public void Stop()
        {
            Logger.Info("pipe server exit signal received, waiting for task shutdown");
            AcceptConnections = false;
            while (Running)
            {
                using NamedPipeClientStream npcs = new NamedPipeClientStream(".", PipeName + PipeMessage.DefaultPipeCommand, PipeDirection.Out);
                try
                {
                    npcs.Connect(100);
                }
                catch (Exception)
                {
                    Logger.Warn("pipe server shutdown signal failed, retrying...");
                }
            }
            Task.Wait();
            Logger.Info("pipe server shutdown confirmed");
        }

        private void SendResponse(string message)
        {
            bool canClose = false;
            Task.Run(() =>
            {
                using NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName + PipeMessage.DefaultPipeResponse, PipeDirection.Out);
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
                        using NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName + PipeMessage.DefaultPipeResponse, PipeDirection.In);
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