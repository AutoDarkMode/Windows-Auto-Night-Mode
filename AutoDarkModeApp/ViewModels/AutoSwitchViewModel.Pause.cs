using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Pause and Postpone management
public partial class AutoSwitchViewModel : ObservableRecipient
{
    private sealed class PauseStateResult
    {
        public PauseMode Mode { get; set; }
        public int? Minutes { get; set; }
        public string? InfoText { get; set; } = string.Empty;
        public bool ResumeEnabled { get; set; }
    }

    private void LoadPauseTimer(object? sender, EventArgs e)
    {
        _isInitializing = true;

        // Run network / parsing code in a background thread to avoid blocking the UI
        Task.Run(() =>
        {
            PauseStateResult result = new PauseStateResult();

            try
            {
                ApiResponse reply = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.GetPostponeStatus));

                // Time-out
                if (reply.StatusCode == StatusCode.Timeout)
                {
                    result.Mode = PauseMode.Off;
                    result.Minutes= null;
                    //UpdateInfoText();
                    result.InfoText = "Statuscode: Timeout";
                    _isInitializing = false;
                    return;
                }
                // Disabled
                else if (reply.StatusCode == StatusCode.Disabled)
                {
                    result.Mode = PauseMode.Off;
                    result.Minutes = null;
                    //PauseInfoText = "Msg_AutoSwitchDisabled".GetLocalized();
                    result.InfoText = "Statuscode: Disabled";
                    //UpdateInfoText();
                    _isInitializing = false;
                    return;
                }
                else
                {
                    if (reply.Message == "True")
                    {
                        bool anyNoExpiry = false;
                        bool canResume = false;

                        PostponeQueueDto dto = PostponeQueueDto.Deserialize(reply.Details);

                        // build list
                        List<string> localisedItems = dto.Items.Select(item =>
                        {
                            if (item.Expiry == null) anyNoExpiry = true;
                            if (item.IsUserClearable) canResume = true;

                            item.SetCulture(new CultureInfo(
                                Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride));

                            return item.GetLocalizationData().BuildLocalizedString();
                        }).ToList();

                        // Determine PauseMode based on the items in the queue
                        if (dto.Items.Any(i => i.Expiry == null))
                        {
                            result.Mode = PauseMode.Once;
                            result.Minutes = null;
                        }
                        else if (dto.Items.Any(i => i.Expiry != null))
                        {
                            result.Mode = PauseMode.Timed;
                            result.Minutes = dto.Items
                                .Where(i => i.Expiry != null)
                                .Select(i => (int)(i.Expiry!.Value - DateTime.Now).TotalMinutes)
                                .Where(minutes => minutes > 0)
                                .FirstOrDefault();
                        }
                        else
                        {
                            result.Mode = PauseMode.Off;
                            result.Minutes = null;
                        }
                            result.InfoText = "ActivePauses".GetLocalized() + ": " + string.Join(", ", localisedItems);
                        result.ResumeEnabled = anyNoExpiry && !canResume;
                    }
                    else
                    {
                        // No active pauses
                        result.Mode = PauseMode.Off;
                        result.Minutes = null;
                        result.InfoText = "Msg_AutoSwitchEnabled".GetLocalized();
                        result.ResumeEnabled = false;
                    }
                }
            }
            catch
            {
                result.Mode = PauseMode.Off;
                result.Minutes = null;
                result.InfoText = "Msg_AutoSwitchEnabled".GetLocalized();
                result.ResumeEnabled = false;
            }

            // Now marshal only the UI updates back to the UI thread
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    ResumeInfoBarEnabled = result.ResumeEnabled;

                    CurrentPauseMode = result.Mode;
                    CurrentPauseMinutes = result.Minutes;
                    PauseInfoText = result.InfoText ?? "error @ TryEnqueue, LoadPauseTimer";
                }
                finally
                {
                    // End initialization on UI thread after all UI properties are set
                    _isInitializing = false;
                }
            });
        });
    }

    partial void OnSelectedPauseIndexChanged(int value)
    {
        if (_isInitializing)
            return;

        UpdatePauseState(value);
    }

    private void UpdatePauseState(int index)
    {
        switch (index)
        {
            case 0: // Off
                CurrentPauseMode = PauseMode.Off;
                CurrentPauseMinutes = null;
                SendPauseOff();
                break;
            case 1: // Once
                CurrentPauseMode = PauseMode.Once;
                CurrentPauseMinutes = null;
                SendPauseOnce();
                break;
            case 2: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 15; SendPauseTimed(15); break;
            case 3: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 30; SendPauseTimed(30); break;
            case 4: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 60; SendPauseTimed(60); break;
            case 5: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 120; SendPauseTimed(120); break;
            case 6: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 240; SendPauseTimed(240); break;
            case 7: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 480; SendPauseTimed(480); break;
            case 8: CurrentPauseMode = PauseMode.Timed; CurrentPauseMinutes = 720; SendPauseTimed(720); break; // 12h
        }

        UpdateInfoText();
    }

    private void SendPauseOff()
    {
        MessageHandler.Client.SendMessageAndGetReply(Command.ClearPostponeQueue);
    }

    private void SendPauseOnce()
    {
        SendPauseOff();
        MessageHandler.Client.SendMessageAndGetReply(Command.ToggleSkipNext);
    }

    private void SendPauseTimed(int minutes)
    {
        SendPauseOff();
        MessageHandler.Client.SendMessageAndGetReply($"{Command.DelayBy} {minutes}");
    }

    private void UpdateInfoText()
    {
        switch (CurrentPauseMode)
        {
            case PauseMode.Off:
                PauseInfoText = "Msg_AutoSwitchEnabled".GetLocalized();
                break;
            case PauseMode.Once:
                PauseInfoText = "PauseMode_Once".GetLocalized();
                break;
            case PauseMode.Timed:
                PauseInfoText = string.Format("PauseMode_Timed".GetLocalized(), CurrentPauseMinutes);
                break;
        }
    }
}
