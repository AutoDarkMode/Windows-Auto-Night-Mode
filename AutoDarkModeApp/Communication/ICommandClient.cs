using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Communication
{
    interface ICommandClient
    {
        public bool SendMessage(string message);
        public string SendMessageAndGetReply(string message);

        public Task<bool> SendMessageAsync(string message);
        public Task<string> SendMesssageAndGetReplyAsync(string message);
    }
}
