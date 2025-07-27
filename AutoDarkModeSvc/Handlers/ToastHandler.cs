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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;


// https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-progress-bar?tabs=builder-syntax

namespace AutoDarkModeSvc.Handlers;

public static class ToastHandler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    private static readonly GlobalState state = GlobalState.Instance();

    /// <summary>
    /// Performs all logic related to showing the delay notifiction for auto-theme switching. <br/>
    /// Will only show a toast and delay if another notficiation still has its grace period delay active, or if another theme switch pause is active. <br/>
    /// </summary>
    public static void InvokeDelayAutoSwitchNotifyToast()
    {
        try
        {
            if (state.PostponeManager.Get(Helper.PostponeItemDelayGracePeriod) != null) return;
            if (state.PostponeManager.IsSkipNextSwitch || state.PostponeManager.IsUserDelayed)
            {
                Logger.Info("another auto pause is already active, skipping toast and grace delay");
                state.PostponeManager.Remove(Helper.PostponeItemDelayGracePeriod);
                return;
            }
            Logger.Info($"requested theme at delay notification time: {Enum.GetName(state.InternalTheme).ToLower()}");

            state.PostponeManager.Add(new(Helper.PostponeItemDelayGracePeriod, DateTime.Now.AddMinutes(builder.Config.AutoSwitchNotify.GracePeriodMinutes), SkipType.Unspecified));

            // retrieve data on whether to show sunrise/sunset in the postpone combobox.
            (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
            string until = skipType == SkipType.UntilSunset ? Strings.Resources.UntilSunset : Strings.Resources.UntilSunrise;

            Program.ActionQueue.Add(() =>
            {
                new ToastContentBuilder()
                .AddText(Strings.Resources.Msg_SwitchPending)
                .AddText(Strings.Resources.Msg_PendingQuestion)
                .AddToastInput(new ToastSelectionBox("time")
                {
                    DefaultSelectionBoxItemId = "15",
                    Items =
                    {
                    new ToastSelectionBoxItem("15", Strings.Resources.PostponeTime_15),
                    new ToastSelectionBoxItem("30", Strings.Resources.PostponeTime_30),
                    new ToastSelectionBoxItem("60", Strings.Resources.PostponeTime_60),
                    new ToastSelectionBoxItem("180", Strings.Resources.PostponeTime_180),
                    new ToastSelectionBoxItem("next", until)
                    }
                })
                .AddButton(new ToastButton(Strings.Resources.SwitchNow, "switch-now"))
                .AddButton(new ToastButton(Strings.Resources.Delay, "delay"))
                .AddArgument("delay")
                .Show(toast =>
                {
                    toast.Tag = "adm-theme-switch-delayed-notif";
                    toast.ExpirationTime = DateTime.Now.AddMinutes(builder.Config.AutoSwitchNotify.GracePeriodMinutes);
                });
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "error invoking auto switch notify toast: ");
        }
    }


    public static void InvokePauseOnToggleThemeToast()
    {
        if (state.PostponeManager.IsSkipNextSwitch)
        {
            Program.ActionQueue.Add(() =>
            {
                ToastContentBuilder tcb = new();
                PostponeItem item = state.PostponeManager.GetSkipNextSwitchItem();
                if (item.Expires)
                {
                    DateTime time = state.PostponeManager.GetSkipNextSwitchItem().Expiry ?? DateTime.Now;
                    if (item.Expiry.Value.Day > DateTime.Now.Day) tcb.AddText($"{Strings.Resources.Msg_PauseHeader} {time:dddd HH:mm}");
                    else tcb.AddText($"{Strings.Resources.Msg_PauseHeader} {time:HH:mm}");
                }
                else
                {
                    (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
                    string until = skipType == SkipType.UntilSunset ? Strings.Resources.UntilSunset : Strings.Resources.UntilSunrise;

                    tcb.AddText($"{Strings.Resources.Msg_PauseOnce}\n({until})");
                }

                tcb.AddText($"{Strings.Resources.Msg_PauseActionNotification} {Strings.Resources.Msg_PauseActionDisableQuestion}")
                   .AddButton(new ToastButton().SetContent(Strings.Resources.DisableAutomaticSwitch)
                   .AddArgument("action-toggle-auto-theme-switch", "enabled")
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

    public static void InvokeAutoSwitchToggleToast()
    {
        if (builder.Config.Notifications.OnAutoThemeSwitching)
        {
            Program.ActionQueue.Add(() =>
            {
                string currentAutoThemeSwitchState = builder.Config.AutoThemeSwitchingEnabled ? Strings.Resources.Enabled.ToLower() : Strings.Resources.Disabled.ToLower();
                string autoThemeSwitchStateArgument = builder.Config.AutoThemeSwitchingEnabled ? "enabled" : "disabled";
                string toastText = $"{Strings.Resources.RevertAction}";

                ToastContentBuilder tcb = new ToastContentBuilder()
                    .AddText($"{Strings.Resources.AutomaticThemeSwitch} {currentAutoThemeSwitchState}");

                if (builder.Config.AutoThemeSwitchingEnabled)
                {
                    tcb.AddText(toastText += $" {Strings.Resources.RequestSwitchAction}");
                    tcb.AddButton(new ToastButton().SetContent(Strings.Resources.Confirm).AddArgument("action", "request-switch"));
                }
                else
                {
                    tcb.AddText(toastText);
                }

                tcb.AddButton(new ToastButton().SetContent(Strings.Resources.Undo).AddArgument("action-toggle-auto-theme-switch", autoThemeSwitchStateArgument));
                tcb.Show(toast =>
                {
                    toast.Tag = "adm-auto-switch-disabled-notif";
                    toast.ExpirationTime = DateTime.Now.AddHours(1);
                });

            });
        }
    }

    public static void InvokePauseAutoSwitchToast()
    {
        if (!builder.Config.Notifications.OnSkipNextSwitch) return;
        Program.ActionQueue.Add(() =>
        {

            ToastContentBuilder tcb = new ToastContentBuilder();

            if (state.PostponeManager.IsSkipNextSwitch)
            {
                PostponeItem item = state.PostponeManager.GetSkipNextSwitchItem();
                if (item.Expires)
                {
                    DateTime time = state.PostponeManager.GetSkipNextSwitchItem().Expiry ?? DateTime.Now;
                    if (item.Expiry.Value.Day > DateTime.Now.Day) tcb.AddText($"{Strings.Resources.Msg_PauseHeader} {time:dddd HH:mm}");
                    else tcb.AddText($"{Strings.Resources.Msg_PauseHeader} {time:HH:mm}");
                }
                else
                {
                    (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
                    string until = skipType == SkipType.UntilSunset ? Strings.Resources.UntilSunset : Strings.Resources.UntilSunrise;
                    tcb.AddText($"{Strings.Resources.Msg_PauseOnce}\n({until})");
                }
                string toastText = $"{Strings.Resources.RevertAction}";
                tcb.AddText(toastText);
            }
            else
            {
                tcb.AddText($"{Strings.Resources.Msg_ResumeHeader}");
                tcb.AddText($"{Strings.Resources.RevertAction}");
            }

            tcb.AddButton(new ToastButton().SetContent(Strings.Resources.Undo).AddArgument("action-toggle-pause-theme-switch", state.PostponeManager.IsSkipNextSwitch));

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
                .AddText($"{Strings.Resources.UpdateToast_PatchingFailed}")
                .AddText($"{Strings.Resources.UpdateToast_AnErrorOccuredPatching}")
                .AddText($"{Strings.Resources.UpdateToast_SeeLogs}")
                 .AddButton(new ToastButton()
                 .SetContent(Strings.Resources.UpdateToast_ButtonOpenLogDirectory)
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
        string typeVerb = downgrade ? Strings.Resources.UpdateToast_DowngradingTo : Strings.Resources.UpdateToast_UpgradingTo;
        Program.ActionQueue.Add(() =>
        {
            // Define a tag (and optionally a group) to uniquely identify the notification, in order update the notification data later
            string tag = "adm_update_in_progress";
            string group = "downloads";

            // Construct the toast content with data bound fields
            ToastContent content = new ToastContentBuilder()
                .AddText($"{typeVerb} {version}")
                .AddVisualChild(new AdaptiveProgressBar()
                {
                    Title = $"{Strings.Resources.UpdateToast_DownloadInProgress}",
                    Value = new BindableProgressBarValue("progressValue"),
                    ValueStringOverride = new BindableString("progressValueString"),
                    Status = new BindableString("progressStatus")
                })
                .GetToastContent();

            // Generate the toast notification
            ToastNotification toast = new(content.GetXml())
            {
                Tag = tag,
                Group = group,
                Data = new NotificationData()
            };
            toast.Data.Values["progressValue"] = "0.0";
            toast.Data.Values["progressValueString"] = "0 MB";
            toast.Data.Values["progressStatus"] = Strings.Resources.UpdateToast_Downloading;
            toast.Data.SequenceNumber = 0; // Provide sequence number to prevent out-of-order updates, or assign 0 to indicate "always update"

            ToastNotificationManagerCompat.History.Remove("adm_update");
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
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
        string versionTag = UpdateHandler.UpstreamVersion.Tag;
        if (UpdateHandler.IsARMUpgrade)
        {
            versionTag += " (ARM64)";
        }

        string updateString = downgrade ? string.Format(Strings.Resources.UpdateToast_DowngradeAvailable, versionTag) : string.Format(Strings.Resources.UpdateToast_NewVersionAvailable, versionTag);
        string updateAction = downgrade ? "downgrade" : "update";
        string updateButton = downgrade ? Strings.Resources.UpdateToast_ButtonDowngrade : Strings.Resources.UpdateToast_ButtonUpdate;

        Program.ActionQueue.Add(() =>
        {

            if (canUseUpdater)
            {
                new ToastContentBuilder()
               .AddText(updateString)
               .AddText($"{Strings.Resources.UpdateToast_CurrentVersion}: {Assembly.GetExecutingAssembly().GetName().Version}")
               .AddText($"{Strings.Resources.UpdateToast_Message}: {UpdateHandler.UpstreamVersion.Message}")
               .AddButton(new ToastButton()
               .SetContent(updateButton)
               .AddArgument("action", updateAction))
               .AddButton(new ToastButton()
               .SetContent(Strings.Resources.UpdateToast_ButtonPostpone)
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
               .AddText($"{updateString}")
               .AddText($"{Strings.Resources.UpdateToast_CurrentVersion}: {Assembly.GetExecutingAssembly().GetName().Version}")
               .AddText($"{Strings.Resources.UpdateToast_Message}: {UpdateHandler.UpstreamVersion.Message}")
               .AddButton(new ToastButton()
                 .SetContent(Strings.Resources.UpdateToast_GoToDownloadPage)
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
                    Task.Run(() => _ = UpdateHandler.Downgrade(overrideSilent: true)).Wait();
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
                    state.PostponeManager.RemoveSkipNextSwitch();
                }
                else if (argument[0] == "action-toggle-auto-theme-switch")
                {

                    AdmConfig old = builder.Config;
                    if (argument[1] == "enabled")
                    {
                        Logger.Info("enable auto theme switch via toast");
                        builder.Config.AutoThemeSwitchingEnabled = false;
                    }
                    else if (argument[1] == "disabled")
                    {
                        Logger.Info("disable auto theme switch via toast");
                        builder.Config.AutoThemeSwitchingEnabled = true;
                    }
                    try
                    {
                        state.SkipConfigFileReload = true;
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "couldn't save config file: ");
                    }
                }
                else if (argument[0] == "action-toggle-pause-theme-switch")
                {
                    if (argument[1] == "0")
                    {
                        Logger.Info("skip next switch via toast");
                        state.PostponeManager.AddSkipNextSwitch();
                    }
                    else
                    {
                        Logger.Info("clear postpone queue via toast");
                        state.PostponeManager.RemoveUserClearablePostpones();
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
                            state.PostponeManager.Add(new(Helper.PostponeItemDelayAutoSwitch, DateTime.Now.AddMinutes(15), SkipType.Unspecified));
                        }
                        if (userInput.Values.Contains("30"))
                        {
                            Logger.Info("postpone auto switch for 30 minutes via toast");
                            state.PostponeManager.Add(new(Helper.PostponeItemDelayAutoSwitch, DateTime.Now.AddMinutes(30), SkipType.Unspecified));
                        }
                        if (userInput.Values.Contains("60"))
                        {
                            Logger.Info("postpone auto switch for 60 minutes via toast");
                            state.PostponeManager.Add(new(Helper.PostponeItemDelayAutoSwitch, DateTime.Now.AddMinutes(60), SkipType.Unspecified));
                        }
                        if (userInput.Values.Contains("180"))
                        {
                            Logger.Info("postpone auto switch for 180 minutes via toast");
                            state.PostponeManager.Add(new(Helper.PostponeItemDelayAutoSwitch, DateTime.Now.AddMinutes(180), SkipType.Unspecified));
                        }
                        if (userInput.Values.Contains("next"))
                        {
                            Logger.Info("postpone auto switch once via toast");
                            state.PostponeManager.AddSkipNextSwitch();
                        }
                        state.PostponeManager.Remove(Helper.PostponeItemDelayGracePeriod);
                    }
                }
                else if (arguments[0] == "switch-now")
                {
                    Logger.Debug("remove notification switch delay grace period delay via toast");
                    state.PostponeManager.Remove(Helper.PostponeItemDelayGracePeriod);
                    ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "failed parsing toast callback:");
        }

    }
}
