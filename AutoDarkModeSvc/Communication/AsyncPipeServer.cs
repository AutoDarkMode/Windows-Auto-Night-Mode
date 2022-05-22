﻿// This file is part of AutoDarkMode.
// AutoDarkMode is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AutoDarkMode is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with AutoDarkMode.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Communication
{
    class AsyncPipeServer : IMessageServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private CancellationTokenSource WorkerTokenSource { get; set; } = new();
        private Service Service { get; }
        private int WorkerCount { get; set; }
        private int availableWorkers;
        public int AvailableWorkers
        {
            get { return availableWorkers; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set { availableWorkers = value; }
        }
        private BlockingCollection<TimeoutEventWrapper> MonitoredStreams { get; } = new();
        private BlockingCollection<Action> Workers { get; } = new();
        private Task TimeoutHandler { get; set; }
        private Task ConnectionHandler { get; set; }
        private readonly int streamTimeout;

        public AsyncPipeServer(Service service, int numWorkers, int streamTimeout = 5000)
        {
            WorkerCount = numWorkers;
            Service = service;
            this.streamTimeout = streamTimeout;
        }
        public void Start()
        {
            Loop();
        }
        public void Stop()
        {
            Workers.CompleteAdding();
            MonitoredStreams.CompleteAdding();
            WorkerTokenSource.Cancel();
            if (TimeoutHandler != null)
            {
                TimeoutHandler.Wait();
            }
            if (ConnectionHandler != null)
            {
                ConnectionHandler.Wait();
            }
            WorkerTokenSource.Dispose();
            Logger.Info("npipe server stopped");
        }

        private void Loop()
        {
            ConnectionHandler = Task.Run(() =>
            {
                bool notify = false;
                bool allowNotify = false;
                Logger.Info($"started npipe server with {WorkerCount} worker{(WorkerCount == 1 ? "" : "s")} (single in, multi out), " +
                    $"request: {Address.PipePrefix + Address.PipeRequest}");
                try
                {
                    for (int i = 0; i < WorkerCount; i++)
                    {
                        Workers.Add(HandleClient);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "could not instantiate workers");
                    Service.RequestExit(this, EventArgs.Empty);
                }

                // send workers to work whenever they are not blocking
                while (Workers.TryTake(out Action a, -1))
                {
                    try
                    {
                        if (AvailableWorkers == 0 && !notify && allowNotify)
                        {
                            Logger.Warn($"request load saturates worker count ({WorkerCount})");
                            notify = true;
                        }
                        else if (AvailableWorkers > 0 && notify && allowNotify)
                        {
                            Logger.Info($"request load returned to normal");
                            notify = false;
                        }

                        AvailableWorkers += 1;
                        
                        if (AvailableWorkers == WorkerCount)
                        {
                            allowNotify = true;
                            notify = false;
                        }

                        a.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "error in request worker:");
                    }
                }
            });

            TimeoutHandler = Task.Run(async () =>
            {
                while (MonitoredStreams.TryTake(out TimeoutEventWrapper stream, -1))
                {
                    await stream.Monitor();
                }
            });
        }

        private async void HandleClient()
        {
            Tuple<string, string> result = await HandleRequest();
            // if no string was received, add the worker back to the pool
            if (result.Item1 == null)
            {
                TryAddWorker();
                return;
            }
            if (result.Item2 == "")
            {
                HandleResponse(result.Item1, result.Item2);
            }
            else
            {
                HandleResponse(result.Item1, $"_{result.Item2}");
            }
        }

        private async Task<Tuple<string, string>> HandleRequest()
        {
            string msg = null;
            string responderPipeId = "";
            try
            {
                // this stream is the main requester loop and should only be cancelled from the outside if the server is stopped
                using NamedPipeServerStream requestPipe = new(Address.PipePrefix + Address.PipeRequest, PipeDirection.InOut, WorkerCount, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await requestPipe.WaitForConnectionAsync(WorkerTokenSource.Token);
                AvailableWorkers -= 1;
                if (AvailableWorkers == 0)
                {
                    Logger.Debug($"client connected, worker pool exhausted");
                }
                else
                {
                    Logger.Debug($"client connected, available workers: {availableWorkers}");
                }

                // a read operation must be completed within streamTimeout, otherwise the pipe connection will be closed server-side to avoid infinite hanging
                // this is especially important if a client connects, and never writes anything to the stream, this would block a worker until the client terminates
                using CancellationTokenSource readTimeoutTokenSource = new();
                Task tew = new TimeoutEventWrapper(requestPipe, readTimeoutTokenSource.Token).Monitor();
                readTimeoutTokenSource.CancelAfter(streamTimeout);
                if (requestPipe.CanRead)
                {
                    using StreamReader sr = new(requestPipe);

                    // read two lines, the message and the output pipe address
                    if (requestPipe.IsConnected) msg = sr.ReadLine();
                    if (requestPipe.IsConnected) responderPipeId = sr.ReadLine() ?? "";
                    if (msg == null)
                    {
                        Logger.Warn("no message received within request window");
                        return new(null, responderPipeId);
                    }
                    Logger.Debug("received message: {0}, requested response channel: {1}", msg, responderPipeId == "" ? "root" : responderPipeId);
                    // always cancel the monitor thread after a message has been received successfully
                    readTimeoutTokenSource.Cancel();
                    await tew;
                }
                else
                {
                    return new(null, responderPipeId);
                }
            }
            catch (TaskCanceledException)
            {
                return new(null, responderPipeId);
            }
            catch (OperationCanceledException)
            {
                return new(null, responderPipeId);
            }
            catch (IOException ex)
            {
                Logger.Warn("request pipe was closed prematurely");
                Logger.Debug(ex, "exception:");
                return new(null, responderPipeId);
            }
            catch (UnauthorizedAccessException ex)
            {
                await Task.Delay(5000);
                Logger.Error(ex, $"system permission missing to create request pipe, attempting to reinstantiate worker:");
                return new(null, responderPipeId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in npipe server request:");
                return new(null, responderPipeId);
            }
            return new(msg, responderPipeId);
        }

        private async void HandleResponse(string msg, string responderPipeId)
        {
            // for the response timeout, we don't want to wait forever, so it also adheres to streamTimeout
            using CancellationTokenSource connectTimeoutTokenSource = new();
            try
            {
                connectTimeoutTokenSource.CancelAfter(streamTimeout);
                using NamedPipeServerStream responsePipe = new(Address.PipePrefix + Address.PipeResponse + $"{responderPipeId}", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await responsePipe.WaitForConnectionAsync(connectTimeoutTokenSource.Token);

                string response = "";
                MessageParser.Parse(new List<string>() { msg }, (message) =>
                {
                    response = message;
                }, Service);

                try
                {
                    // exppect pipe data to be consumed within the streamTimeout timeframe
                    // if not cancel the write operation
                    using CancellationTokenSource writeTimeoutTokenSource = new();
                    writeTimeoutTokenSource.CancelAfter(streamTimeout);
                    StreamWriter sw = new(responsePipe)
                    { AutoFlush = true };
                    using (sw)
                    {
                        StringBuilder builder = new(response);
                        await sw.WriteAsync(builder, writeTimeoutTokenSource.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    Logger.Warn("no client available to consume data within response window");
                }
            }
            catch (TaskCanceledException)
            {
                Logger.Warn("no client waiting for response, processing request anyway");
                MessageParser.Parse(new List<string>() { msg }, (message) => { }, Service);
            }
            catch (IOException ex)
            {
                Logger.Warn("response pipe was closed prematurely");
                Logger.Debug(ex, "exception:");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in npipe server:");
            }

            TryAddWorker();
        }

        private void TryAddWorker()
        {
            try
            {
                if (!Workers.IsAddingCompleted) Workers.Add(HandleClient);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "permanently lost worker due to error:");
            }
        }
    }



    internal class TimeoutEventWrapper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly NamedPipeServerStream stream;
        private readonly CancellationToken token;
        public TimeoutEventWrapper(NamedPipeServerStream stream, CancellationToken token)
        {
            this.stream = stream;
            this.token = token;
        }

        public async Task Monitor()
        {
            try
            {
                await Task.Delay(-1, token);
            }
            catch (TaskCanceledException)
            {
                if (stream.IsConnected)
                {
                    stream.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while monitoring stream timeout:");
            }
        }

    }
}
