using System.Diagnostics;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeApp.Utils.Handlers;

internal class Updater
{
    private ApiResponse response = new();

    public Updater()
    {
    }

    public bool CheckNewVersion()
    {
        response = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.CheckForUpdateNotify));
        return UpdateAvailable();
    }

    public bool UpdateAvailable()
    {
        if (response.StatusCode == StatusCode.New || response.StatusCode == StatusCode.Disabled)
        {
            return true;
        }
        return false;
    }

    public bool CanUseUpdater()
    {
        return response.StatusCode != StatusCode.Disabled;
    }

    public void Update()
    {
        var info = UpdateInfo.Deserialize(response.Details);
        var updatePrepResponse = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.Update));
        if (updatePrepResponse.StatusCode == StatusCode.New)
        {
            StartProcessByProcessInfo(info.GetUpdateInfoPage());
        }
    }

    /*
    public void MessageBoxHandler(Window owner = null)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.SelectedLanguageCode, true);
        if (UpdateAvailable())
        {
            if (!silent)
            {
                UpdateInfo info = UpdateInfo.Deserialize(response.Details);
                string text = string.Format(AdmProperties.Resources.msgUpdaterText, response.Message, info.Tag);
                MsgBox msgBox = new(text, "Auto Dark Mode Updater", "update", "yesno")
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };
                if (owner != null)
                {
                    msgBox.Owner = owner;
                }
                msgBox.ShowDialog();
                bool? result = msgBox.DialogResult;
                if (result == true)
                {
                    Update();
                }
            }
        }
    }
    */

    /*
    private static void ShowErrorMessage(Exception ex, string location)
    {
        string error = AdmProperties.Resources.ErrorMessageBox_Content + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
        MsgBox msg = new(error, AdmProperties.Resources.ErrorOccurred_Title, "error", "yesno");
        msg.ShowDialog();
        var result = msg.DialogResult;
        if (result == true)
        {
            var issueUri = @"https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/issues";
            Process.Start(new ProcessStartInfo(issueUri)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
        return;
    }
    */

    private static void StartProcessByProcessInfo(string message)
    {
        Process.Start(new ProcessStartInfo(message)
        {
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
