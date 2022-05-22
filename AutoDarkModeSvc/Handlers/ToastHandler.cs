using Windows.UI.Notifications;
using AutoDarkModeConfig;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

// https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-progress-bar?tabs=builder-syntax

namespace AutoDarkModeSvc.Handlers
{
    public class ToastHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void InvokeFailedUpdateToast()
        {
            Program.ActionQueue.Add(() =>
            {
                string configPath = AdmConfigBuilder.ConfigDir;
                new ToastContentBuilder()
                    .AddText($"Patching failed")
                    .AddText($"An error occurred while patching")
                    .AddText($"Please see service.log and updater.log for more infos")
                     .AddButton(new ToastButton()
                     .SetContent("Open log directory")
                     .SetProtocolActivation(new Uri(configPath)))
                    .SetProtocolActivation(new Uri(configPath))
                    .Show(toast =>
                    {
                        toast.Tag = "adm_failed_update";
                    });
            });
        }


        public static void InvokeUpdateInProgressToast(string version, bool downgrade = false)
        {
            string typeVerb = downgrade ? "Downgrading" : "Updating";
            Program.ActionQueue.Add(() =>
            {
                // Define a tag (and optionally a group) to uniquely identify the notification, in order update the notification data later;
                string tag = "adm_update_in_progress";
                string group = "downloads";

                // Construct the toast content with data bound fields
                ToastContent content = new ToastContentBuilder()
                    .AddText($"{typeVerb} to {version}")
                    .AddVisualChild(new AdaptiveProgressBar()
                    {
                        Title = "Download in progress",
                        Value = new BindableProgressBarValue("progressValue"),
                        ValueStringOverride = new BindableString("progressValueString"),
                        Status = new BindableString("progressStatus")
                    })
                    .GetToastContent();

                // Generate the toast notification
                ToastNotification toast = new(content.GetXml());

                // Assign the tag and group
                toast.Tag = tag;
                toast.Group = group;

                // Assign initial NotificationData values
                // Values must be of type string
                toast.Data = new NotificationData();
                toast.Data.Values["progressValue"] = "0.0";
                toast.Data.Values["progressValueString"] = "0 MB";
                toast.Data.Values["progressStatus"] = "Downloading...";

                // Provide sequence number to prevent out-of-order updates, or assign 0 to indicate "always update"
                toast.Data.SequenceNumber = 0;
                ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
                ToastNotificationManagerCompat.History.Remove("adm_update");
            });
        }

        public static void UpdateProgressToast(string progressValue, string progressValueString)
        {
            // INFO: Put in actionqueue if exception is thrown

            // Construct a NotificationData object;
            string tag = "adm_update_in_progress";
            string group = "downloads";

            NotificationData data = new();

            // Assign new values
            // Note that you only need to assign values that changed. In this example
            // we don't assign progressStatus since we don't need to change it
            data.Values["progressValue"] = progressValue;
            data.Values["progressValueString"] = progressValueString;

            // Update the existing notification's data by using tag/group
            ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag, group);
        }

        public static void InvokeUpdateToast(bool canUseUpdater = true, bool downgrade = false)
        {
            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                return;
            }

            string updateString = downgrade ? "Downgrade" : "Update";
            string updateAction = downgrade ? "downgrade" : "update";

            Program.ActionQueue.Add(() =>
            {

                if (canUseUpdater)
                {
                    new ToastContentBuilder()
                   .AddText($"{updateString} {UpdateHandler.UpstreamVersion.Tag} available")
                   .AddText($"Current Version: {Assembly.GetExecutingAssembly().GetName().Version}")
                   .AddText($"Message: {UpdateHandler.UpstreamVersion.Message}")
                   .AddButton(new ToastButton()
                   .SetContent(updateString)
                   .AddArgument("action", updateAction))
                   .SetBackgroundActivation()
                   .AddButton(new ToastButton()
                   .SetContent("Postpone")
                   .AddArgument("action", "postpone"))
                   //.SetBackgroundActivation()
                   //.SetProtocolActivation(new Uri(UpdateInfo.changelogUrl))
                   .SetProtocolActivation(new Uri(UpdateHandler.UpstreamVersion.ChangelogUrl))
                   .Show(toast =>
                   {
                       toast.Tag = "adm_update";
                   });
                }
                else
                {
                    new ToastContentBuilder()
                   .AddText($"{updateString} {UpdateHandler.UpstreamVersion.Tag} available")
                   .AddText($"Current Version: {Assembly.GetExecutingAssembly().GetName().Version}")
                   .AddText($"Message: {UpdateHandler.UpstreamVersion.Message}")
                   .AddButton(new ToastButton()
                     .SetContent("Go to download page")
                     .SetProtocolActivation(new Uri(UpdateHandler.UpstreamVersion.ChangelogUrl)))
                   .SetProtocolActivation(new Uri(UpdateHandler.UpstreamVersion.ChangelogUrl))
                   .Show(toast =>
                   {
                       toast.Tag = "adm_update";
                   });
                }

            });
        }

        public static void RemoveUpdaterToast()
        {
            //Program.ActionQueue.Add(() => ToastNotificationManagerCompat.History.Remove("adm_update_in_progress"));
            ToastNotificationManagerCompat.History.Remove("adm_update_in_progress", "downloads");
            //ToastNotificationManagerCompat.History.Clear();
            //Program.ActionQueue.Add(() => ToastNotificationManagerCompat.History.Clear());
        }

        public static void HandleToastAction(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            try
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;
                if (toastArgs.Argument.Length == 0)
                {
                    return;
                }
                Logger.Debug("toast called with args: " + toastArgs.Argument);
                string[] arguments = toastArgs.Argument.Split(";");
                foreach (string argumentString in arguments)
                {
                    string[] argument = argumentString.Split("=");
                    if (argument[0] == "action" && argument[1] == "update")
                    {
                        Logger.Info("updating app, caller toast");
                        Task.Run(() => UpdateHandler.Update(overrideSilent: true)).Wait();
                    }
                    else if (argument[0] == "action" && argument[1] == "downgrade")
                    {
                        Logger.Info("downgrading app, caller toast");
                        Task.Run(() => _ = UpdateHandler.Downgrade()).Wait();
                    }
                    else if (argument[0] == "action" && argument[1] == "postpone")
                    {
                        Logger.Debug("update postponed");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "updater failed, caller toast:");
            }

        }
    }
}
