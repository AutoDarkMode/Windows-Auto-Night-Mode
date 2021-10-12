using AutoDarkModeComms;
using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeUpdater
{
    static class VersionSpecific
    {
        public static IMessageClient GetMessageClient(FileVersionInfo currentVersionInfo)
        {
            Version needsNetMQ = new Version(10, 0, 0, 26);
            Version current = new Version(currentVersionInfo.FileVersion);
            if (current.CompareTo(needsNetMQ) <= 0)
            {
                return new ZeroMQClient(Address.DefaultPort);
            }
            return new PipeClient();
        }
    }
}
