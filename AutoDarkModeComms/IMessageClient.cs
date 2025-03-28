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
using System.Threading.Tasks;

namespace AutoDarkModeComms
{
    public interface IMessageClient
    {
        /// <summary>
        /// Sends a message via the command interface,
        /// returning the message sent by the server.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeoutSeconds">The seconds to wait before the response is considered timed out</param>
        /// <returns>the message relayed by the server</returns>
        public string SendMessageAndGetReply(string message, int timeoutSeconds = 5);

        /// <summary>
        /// Sends a message asynchronously via the command interface,
        /// returning the message sent by the server.
        /// This is mainly for sending messages in the UI to prevent blocking
        /// </summary>
        /// <param name="message"></param>
        /// <returns>the message relayed by the server</returns>
        /// <param name="timeoutSeconds">The seconds to wait before the response is considered timed out</param>
        public Task<string> SendMessageAndGetReplyAsync(string message, int timeoutSeconds = 5);

        /// <summary>
        /// Sends a message via the command interface,
        /// returning the message sent by the server.
        /// 
        /// </summary>
        /// <param name="message">The message to be sent</param>
        /// <param name="retries">The amount of retries that should be performed before entering a timeout state</param>"
        /// <param name="timeoutSeconds">The seconds to wait on each retry before the response is considered timed out</param>
        /// <returns>the message relayed by the server</returns>
        public string SendMessageWithRetries(string message, int timeoutSeconds = 3, int retries = 3);
    }
}
