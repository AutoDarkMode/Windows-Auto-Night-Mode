using System;
using System.Reflection;
using System.Xml;
using System.Windows;
using System.Globalization;
using System.Diagnostics;
using AutoDarkModeSvc.Communication;
using AutoDarkModeComms;

namespace AutoDarkModeApp
{
    class Updater
    {
        bool silent;
        ApiResponse response = new();
        private readonly ICommandClient commandClient;

        public Updater(bool pSilent)
        {
            commandClient = new ZeroMQClient(Address.DefaultPort);
            this.silent = pSilent;
        }

        public bool CheckNewVersion()
        {
            response = ApiResponse.FromString(commandClient.SendMessageAndGetReply(Command.CheckForUpdate));
            return UpdateAvailable();
        }

        public bool UpdateAvailable()
        {
            if (response.StatusCode == StatusCode.New)
            {
                return true;
            }
            return false;
        }

        public void ParseResponse(string response)
        {
            string[] messages = response.Split(";");
            if (messages[0] == StatusCode.New)
            {
                MessageBoxHandler();
            }
        }

        public void MessageBoxHandler()
        {
            CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language, true);
            if (UpdateAvailable())
            {
                if (!silent)
                {
                    string text = String.Format(Properties.Resources.msgUpdaterText, response.Details, response.Message);
                    MsgBox msgBox = new MsgBox(text, "Auto Dark Mode Updater", "update", "yesno")
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Topmost = true
                    };
                    msgBox.ShowDialog();
                    var result = msgBox.DialogResult;
                    if (result == true)
                    {
                        ApiResponse response = ApiResponse.FromString(commandClient.SendMessageAndGetReply(Command.Update));
                        if (response.StatusCode != StatusCode.New)
                        {
                            Exception ex = new($"could not prepare updater, {response.StatusCode} with message. {response.Message} and details {response.Details}");
                            ShowErrorMessage(ex, "Updater");
                        }
                    }
                }
            }
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
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

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}