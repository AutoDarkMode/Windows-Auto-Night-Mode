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
using AutoDarkModeSvc.Communication;
using System;
using System.Diagnostics;
using System.Windows;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Handlers
{
    public class SwitchThemeException : Exception
    {
        private readonly static string customMessage = "Theme switching is unsuccessful";
        public SwitchThemeException() : base(customMessage)
        {
            this.Source = "SwitchThemeException";
        }

        public SwitchThemeException(string message, string source) : base($"{customMessage}: {message}")
        {
            this.Source = source;
        }
    }

    public class AddAutoStartException : Exception
    {
        public override string Message => "Auto start task could not been set.";

        public AddAutoStartException()
        {
            this.Source = "AutoStartException";
        }

        public AddAutoStartException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }


    public class AutoStartStatusGetException : Exception
    {
        public override string Message => "Auto start info could not be retrievbed.";

        public AutoStartStatusGetException()
        {
            this.Source = "AutoStartException";
        }

        public AutoStartStatusGetException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }

    public class RemoveAutoStartException : Exception
    {
        public override string Message => "Auto start task could not been removed.";

        public RemoveAutoStartException()
        {
            this.Source = "RemoveAutoStartException";
        }

        public RemoveAutoStartException(string message, string source) : base(message)
        {
            this.Source = source;
        }
    }

    public static class ErrorMessageBoxes
    {
        public static void ShowErrorMessageFromApi(ApiResponse response, Exception ex, Window owner)
        {
            string error = $"{AdmProperties.Resources.ErrorMessageBox}\n\n" +
                $"Exception Source: {ex.Source}\n" +
                $"Exception Message: {ex.Message}\n\n" +
                $"API Response:\n" +
                $"Status Code: {response.StatusCode}\n" +
                $"Message: {response.Message}\n" +
                $"Details: {response.Details}";
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = owner
            };
            msg.ShowDialog();
            bool result = msg.DialogResult ?? false;
            if (result)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }

        public static void ShowErrorMessageFromApi(ApiResponse response, Window owner)
        {
            string error = $"{AdmProperties.Resources.ErrorMessageBox}\n\n" +
                $"API Response:\n" +
                $"Status Code: {response.StatusCode}\n" +
                $"Message: {response.Message}\n" +
                $"Details: {response.Details}";
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = owner
            };
            msg.ShowDialog();
            bool result = msg.DialogResult ?? false;
            if (result)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }

        public static void ShowErrorMessage(Exception ex, Window owner, string location, string extraInfo = "")
        {
            string error = AdmProperties.Resources.ErrorMessageBox + $"\n\nError ocurred in: {location}" +
                $"\nSource: {ex.Source}" +
                $"\nMessage: {ex.Message}";
            if (extraInfo.Length > 0)
            {
                error += $"\nExtra Detail: {extraInfo}";
            }
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = owner
            };
            msg.ShowDialog();
            bool result = msg.DialogResult ?? false;
            if (result)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }

            return;
        }
    }
}
