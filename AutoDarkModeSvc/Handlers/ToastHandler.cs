using Windows.UI.Notifications;
using AutoDarkModeLib;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Monitors;
using System.Globalization;
using AdmProperties = AutoDarkModeLib.Properties;
using System.Collections.Generic;


// https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-progress-bar?tabs=builder-syntax

namespace AutoDarkModeSvc.Handlers
{
    public static class ToastHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private static readonly GlobalState state = GlobalState.Instance();


        public static void InvokeDelayAutoSwitchNotificationToast()
        {
            if (state.PostponeManager.IsSkipNextSwitch) return;

            (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
            string until = skipType == SkipType.Sunrise ? AdmProperties.Resources.ThemeSwitchPauseUntilSunset : AdmProperties.Resources.ThemeSwitchPauseUntilSunrise;

            Program.ActionQueue.Add(() =>
            {
                new ToastContentBuilder()
                .AddText("Auto theme switch pending...")
                .AddText("Would you like to switch now or postpone it further?")
                .AddToastInput(new ToastSelectionBox("time")
                {
                    DefaultSelectionBoxItemId = "15",
                    Items =
                    {
                        new ToastSelectionBoxItem("15", "15 minutes"),
                        new ToastSelectionBoxItem("30", "30 minutes"),
                        new ToastSelectionBoxItem("60", "1 hour"),
                        new ToastSelectionBoxItem("180", "3 hours"),
                        new ToastSelectionBoxItem("next", until)
                    }
                })
                .AddButton(new ToastButton("Switch now", "switch-now"))
                .AddButton(new ToastButton("Delay", "delay"))
                .Show(toast =>
                {
                    toast.Tag = "adm-theme-switch-delayed-notif";
                    toast.ExpirationTime = DateTime.Now.AddMinutes(builder.Config.AutoSwitchNotify.GracePeriodMinutes);
                });
            });
        }


        public static void InvokeTogglePauseNotificationToast()
        {
            if (state.PostponeManager.IsSkipNextSwitch)
            {
                Program.ActionQueue.Add(() =>
                {
                    ToastContentBuilder tcb = new();

                    if (state.PostponeManager.GetSkipNextSwitchItem().Expires)
                    {
                        DateTime time = state.PostponeManager.GetSkipNextSwitchItem().Expiry ?? DateTime.Now;
                        tcb.AddText($"{AdmProperties.Resources.ThemeSwitchPauseHeader} {time:HH:mm}");
                    }
                    else
                    {
                        (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
                        string until = skipType == SkipType.Sunrise ? AdmProperties.Resources.ThemeSwitchPauseUntilSunset : AdmProperties.Resources.ThemeSwitchPauseUntilSunrise;

                        tcb.AddText($"{AdmProperties.Resources.ThemeSwitchPauseHeaderNoExpiry}\n({until})");
                    }
                    tcb.AddText($"{AdmProperties.Resources.ThemeSwitchPauseActionNotification} {AdmProperties.Resources.ThemeSwitchPauseActionDisableQuestion}")
                       .AddButton(new ToastButton().SetContent(AdmProperties.Resources.ThemeSwitchActionDisable)
                       .AddArgument("action-undo-toggle-theme-switch", "enabled")
                       .AddArgument("action", "remove-skip-next")).Show(toast =>
                    {
                        toast.Tag = "adm-theme-switch-paused-notif";
                        toast.ExpirationTime = DateTime.Now.AddHours(1);
                    });
                });
            }
            else
            {
                ToastNotificationManagerCompat.History.Remove("adm-theme-switch-paused-notif");
            }
        }

        public static void InvokeAutoSwitchNotificationToast()
        {
            if (builder.Config.Notifications.OnAutoThemeSwitching)
            {
                Program.ActionQueue.Add(() =>
                {
                    string currentAutoThemeSwitchState = builder.Config.AutoThemeSwitchingEnabled ? AdmProperties.Resources.enabled.ToLower() : AdmProperties.Resources.disabled.ToLower();
                    string toastText = $"{AdmProperties.Resources.RevertAction}";

                    ToastContentBuilder tcb = new ToastContentBuilder()
                        .AddText($"{AdmProperties.Resources.AutomaticThemeSwitch} {currentAutoThemeSwitchState}");

                    if (builder.Config.AutoThemeSwitchingEnabled)
                    {
                        tcb.AddText(toastText += $" {AdmProperties.Resources.RequestSwitchAction}");
                        tcb.AddButton(new ToastButton().SetContent("Hit me!").AddArgument("action", "request-switch"));
                    }
                    else
                    {
                        tcb.AddText(toastText);
                    }

                    tcb.AddButton(new ToastButton().SetContent(AdmProperties.Resources.ThemeSwitchActionUndo).AddArgument("action-undo-toggle-theme-switch", currentAutoThemeSwitchState));
                    tcb.Show(toast =>
                    {
                        toast.Tag = "adm-auto-switch-disabled-notif";
                        toast.ExpirationTime = DateTime.Now.AddHours(1);
                    });

                });
            }
        }

        public static void InvokePauseNotificationToast()
        {
            if (!builder.Config.Notifications.OnSkipNextSwitch) return;
            Program.ActionQueue.Add(() =>
            {

                ToastContentBuilder tcb = new ToastContentBuilder();

                if (state.PostponeManager.IsSkipNextSwitch)
                {
                    if (state.PostponeManager.GetSkipNextSwitchItem().Expires)
                    {
                        DateTime time = state.PostponeManager.GetSkipNextSwitchItem().Expiry ?? DateTime.Now;
                        tcb.AddText($"{AdmProperties.Resources.ThemeSwitchPauseHeader} {time:HH:mm}");
                    }
                    else
                    {
                        (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
                        string until = skipType == SkipType.Sunrise ? AdmProperties.Resources.ThemeSwitchPauseUntilSunset : AdmProperties.Resources.ThemeSwitchPauseUntilSunrise;
                        tcb.AddText($"{AdmProperties.Resources.ThemeSwitchPauseHeaderNoExpiry}\n({until})");
                    }
                    string toastText = $"{AdmProperties.Resources.RevertAction}";
                    tcb.AddText(toastText);
                }
                else
                {
                    tcb.AddText($"{AdmProperties.Resources.ThemeSwitchResumeHeader}");
                    tcb.AddText($"{AdmProperties.Resources.RevertAction}");
                }

                tcb.AddButton(new ToastButton().SetContent(AdmProperties.Resources.ThemeSwitchActionUndo).AddArgument("action-undo-pause-theme-switch", state.PostponeManager.IsSkipNextSwitch));

                tcb.Show(toast =>
                {
                    toast.Tag = "adm-pause-switch-notif";
                    toast.ExpirationTime = DateTime.Now.AddHours(1);
                });
            });

        }

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
                    else if (argument[0] == "action" && argument[1] == "request-switch")
                    {
                        Logger.Info("request theme switch via toast");
                        ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                    }
                    else if (argument[0] == "action" && argument[1] == "remove-skip-next")
                    {
                        state.PostponeManager.RemoveUserClearablePostpones();
                    }
                    else if (argument[0] == "action-undo-toggle-theme-switch")
                    {

                        AdmConfig old = builder.Config;
                        if (argument[1] == "enabled")
                        {
                            Logger.Info("undo enable auto theme switch via toast");
                            builder.Config.AutoThemeSwitchingEnabled = false;
                        }
                        else if (argument[1] == "disabled")
                        {
                            Logger.Info("undo disable auto theme switch via toast");
                            builder.Config.AutoThemeSwitchingEnabled = true;
                        }
                        try
                        {
                            state.SkipConfigFileReload = true;
                            AdmConfigMonitor.Instance().PerformConfigUpdate(old, internalUpdate: true);
                            builder.Save();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "couldn't save config file: ");
                        }
                    }
                    else if (argument[0] == "action-undo-pause-theme-switch")
                    {
                        if (argument[1] == true.ToString())
                        {
                            state.PostponeManager.RemoveUserClearablePostpones();
                        }
                        else
                        {
                            state.PostponeManager.AddSkipNextSwitch();
                        }
                    }
                    else if (arguments[0] == "delay")
                    {
                        if (userInput.Keys.Count > 0 && userInput.Values.Count > 0)
                        {
                            List<string> keys = new(userInput.Keys);
                            if (userInput.Values.Contains("15"))
                            {
                                Logger.Info("postpone auto switch for 15 minutes via toast");
                                state.PostponeManager.Add(new(Helper.DelaySwitchItemName, DateTime.Now.AddMinutes(15), SkipType.Unspecified));
                            }
                            if (userInput.Values.Contains("30"))
                            {
                                Logger.Info("postpone auto switch for 30 minutes via toast");
                                state.PostponeManager.Add(new(Helper.DelaySwitchItemName, DateTime.Now.AddMinutes(30), SkipType.Unspecified));
                            }
                            if (userInput.Values.Contains("60"))
                            {
                                Logger.Info("postpone auto switch for 60 minutes via toast");
                                state.PostponeManager.Add(new(Helper.DelaySwitchItemName, DateTime.Now.AddMinutes(60), SkipType.Unspecified));
                            }
                            if (userInput.Values.Contains("180"))
                            {
                                Logger.Info("postpone auto switch for 180 minutes via toast");
                                state.PostponeManager.Add(new(Helper.DelaySwitchItemName, DateTime.Now.AddMinutes(180), SkipType.Unspecified));
                            }
                            if (userInput.Values.Contains("next"))
                            {
                                Logger.Info("postpone auto switch once via toast");
                                state.PostponeManager.AddSkipNextSwitch();
                            }
                            state.PostponeManager.Remove(Helper.DelayGracePeriodItemName);
                        }
                    }
                    else if (arguments[0] == "switch-now")
                    {
                        Logger.Info("remove notification switch delay grace period delay via toast");
                        state.PostponeManager.Remove(Helper.DelayGracePeriodItemName);
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
