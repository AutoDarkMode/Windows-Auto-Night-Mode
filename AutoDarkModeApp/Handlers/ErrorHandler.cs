using AutoDarkModeSvc.Communication;
using System;
using System.Diagnostics;
using System.Windows;

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
            string error = $"{Properties.Resources.errorThemeApply}\n\n" +
                $"Exception Source: {ex.Source}\n" +
                $"Exception Message: {ex.Message}\n\n" +
                $"API Response:\n" +
                $"Status Code: {response.StatusCode}\n" +
                $"Message: {response.Message}\n" +
                $"Details: {response.Details}";
            MsgBox msg = new(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
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
