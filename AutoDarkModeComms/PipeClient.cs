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
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeComms;

public class PipeClient : IMessageClient
{
    public string SendMessageAndGetReply(string message, int timeoutSeconds = 5)
    {
        string pipeId = $"C#_{Convert.ToBase64String(Guid.NewGuid().ToByteArray())}";
        using NamedPipeClientStream clientPipeRequest = new(".", Address.PipePrefix + Address.PipeRequest, PipeDirection.Out);
        try
        {
            clientPipeRequest.Connect(timeoutSeconds * 1000);
            StreamWriter sw = new(clientPipeRequest) { AutoFlush = true };
            using (sw)
            {
                sw.WriteLine(message);
                sw.WriteLine(pipeId);
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse()
            {
                StatusCode = StatusCode.Timeout,
                Message = "The service did not acknowledge the req in time",
                Details = $"{ex.GetType()} {ex.Message}"
            }.ToString();
        }

        using NamedPipeClientStream clientPipeResponse = new(".", Address.PipePrefix + Address.PipeResponse + $"_{pipeId}", PipeDirection.In);
        try
        {
            clientPipeResponse.Connect(timeoutSeconds * 1000);
            if (clientPipeResponse.IsConnected && clientPipeResponse.CanRead)
            {
                using StreamReader sr = new(clientPipeResponse);
                //sr.BaseStream.ReadTimeout = timeoutSeconds * 1000;
                string msg = sr.ReadToEnd();
                if (msg == null)
                {
                    return StatusCode.Timeout;
                }
                return msg;
            }
            else
            {
                return new ApiResponse()
                {
                    StatusCode = StatusCode.Err,
                    Message = "Pipe not connected or can't read"
                }.ToString();
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse()
            {
                StatusCode = StatusCode.Timeout,
                Message = "The service did not respond in time",
                Details = $"{ex.GetType()} {ex.Message}"
            }.ToString();
        }
    }

    public Task<string> SendMessageAndGetReplyAsync(string message, int timeoutSeconds = 5)
    {
        return Task.Run(() => SendMessageAndGetReply(message, timeoutSeconds));
    }

    public string SendMessageWithRetries(string message, int timeoutSeconds = 3, int retries = 3)
    {
        return SendMessageAndGetReply(message, timeoutSeconds * retries);
    }
}
