using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeComms
{
    public interface ICommandClient
    {
        /// <summary>
        /// Sends a message via the command infrastructure
        /// </summary>
        /// <param name="message"></param>
        /// <returns>true if successful; false otherwise</returns>
        public bool SendMessage(string message);

        /// <summary>
        /// Sends a message via the command interface,
        /// returning the message sent by the server.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>the message relayed by the server</returns>
        public string SendMessageAndGetReply(string message);

        /// <summary>
        /// Sends a message asynchronously via the command interface. 
        /// This is mainly for sending messages in the UI to prevent blocking
        /// </summary>
        /// <param name="message"></param>
        /// <returns>true if successful; false otherwise</returns>
        public Task<bool> SendMessageAsync(string message);

        /// <summary>
        /// Sends a message asynchronously via the command interface,
        /// returning the message sent by the server.
        /// This is mainly for sending messages in the UI to prevent blocking
        /// </summary>
        /// <param name="message"></param>
        /// <returns>the message relayed by the server</returns>
        public Task<string> SendMessageAndGetReplyAsync(string message);
    }
}
