using AutoDarkModeComms;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoDarkModeApp.Handlers
{
    public static class AutostartHandler
    {
        public static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        public static void EnsureAutostart(Window owner)
        {
            ApiResponse result = new()
            {
                StatusCode = StatusCode.Err,
                Message = "error in frontend: EnsureAutostart()"
            };
            try
            {
                _ = MessageHandler.Client.SendMessageAndGetReply(Command.ValidateAutostart);
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(result, ex, owner);
            }
        }


        /// <summary>
        /// Autostart
        /// </summary>
        public static async void EnableAutoStart(Window owner)
        {
            ApiResponse result = new()
            {
                StatusCode = StatusCode.Err,
                Message = "error in frontend: EnableAutostart()"
            };
            try
            {
                result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                if (result.StatusCode != StatusCode.Ok)
                {
                    throw new AddAutoStartException($"Could not add Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(result, ex, owner);
            }
        }
    }
}
