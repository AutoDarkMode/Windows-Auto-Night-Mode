using AutoDarkModeComms;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Handlers
{
    public static class AutostartHandler
    {
        public  static void EnsureAutostart()
        {
            try
            {
                AdmConfigBuilder builder = AdmConfigBuilder.Instance();
                builder.Load();
                if (builder.Config.AutoThemeSwitchingEnabled)
                {
                    ICommandClient client = new ZeroMQClient(Address.DefaultPort);
                    _ = client.SendMessageAndGetReply(Command.ValidateAutostart);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
