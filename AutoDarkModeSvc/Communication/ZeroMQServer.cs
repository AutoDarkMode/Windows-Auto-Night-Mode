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

/*
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO.MemoryMappedFiles;

namespace AutoDarkModeSvc.Communication
{
    class ZeroMQServer : IMessageServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string Port { get; }
        private Service Service { get; }
        private ResponseSocket ServerSocket { set; get; }
        private NetMQPoller Poller { get; set; }
        private Task PollTask { get; set; }
        private readonly MemoryMappedFile _portshare;

        /// <summary>
        /// Instantiate a ZeroMQ server for socket based messaging
        /// </summary>
        /// <param name="port">the port to use by the server</param>
        /// <param name="service">Service object to pass through to a MessageParser</param>
        public ZeroMQServer(string port, Service service)
        {
            Port = port;
            Service = service;
        }

        public ZeroMQServer(Service service)
        {
            Port = "0";
            _portshare = MemoryMappedFile.CreateNew("adm-backend-port", sizeof(int));
            Service = service;
        }

        /// <summary>
        /// Start the ZeroMQ server
        /// </summary>
        public void Start()
        {
            if (Port.Length != 0)
            {
                ServerSocket = new ResponseSocket();
                int randomPort = ServerSocket.BindRandomPort("tcp://127.0.0.1");
                using MemoryMappedViewAccessor viewAccessor = _portshare.CreateViewAccessor();
                byte[] bytes = BitConverter.GetBytes(randomPort);
                try
                {
                    viewAccessor.WriteArray(0, bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Logger.Error("could not bind socket to port: {0}", ex.Message);
                }
                Logger.Info("socket bound to port: {0}", randomPort);
            }
            else
            {
                ServerSocket = new ResponseSocket("tcp://127.0.0.1:" + Port);
                Logger.Info("socket bound to port: {0}", Port);
            }

            Poller = new NetMQPoller { ServerSocket };
            ServerSocket.ReceiveReady += ReceiveReadyEvent;
            PollTask = Task.Run(() =>
            {
                try
                {
                    Poller.Run();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "ZMQ Poller died");
                    Environment.Exit(-1);
                }
            });
            Logger.Info("started server (polling)");
        }

        /// <summary>
        /// Stop the ZeroMQ Server and release all resources (including the NetMQ Cleanup)
        /// </summary>
        public void Stop()
        {
            Logger.Info("stopping server");
            Poller.Stop();
            PollTask.Wait();
            try
            {
                Poller.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "could not dispose Poller");
            }
            ServerSocket.Dispose();
            _portshare.Dispose();
            NetMQConfig.Cleanup();
        }

        public void ReceiveReadyEvent(object sender, NetMQSocketEventArgs a)
        {
            string msg = "";
            try
            {
                msg = a.Socket.ReceiveFrameString();
                Logger.Debug("received message: {0}", msg);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while receiving message:");
            }

            try
            {

                MessageParser.Parse(new List<string>() { msg }, (message) =>
                {
                    bool sent = a.Socket.TrySendFrame(new TimeSpan(10000000), message);
                    if (!sent)
                    {
                        Logger.Error("could not send response: timeout");
                    }
                }, Service);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"exception while processing command {msg}");
                try
                {
                    bool sent = a.Socket.TrySendFrame(new TimeSpan(10000000), new ApiResponse()
                    {
                        StatusCode = StatusCode.Err,
                        Message = ex.Message
                    }.ToString());
                    if (!sent)
                    {
                        Logger.Error("could not send response: timeout");
                    }
                }
                catch (Exception exErr)
                {
                    Logger.Error(exErr, "could not send error response:");
                }

            }
        }
    }
}
*/