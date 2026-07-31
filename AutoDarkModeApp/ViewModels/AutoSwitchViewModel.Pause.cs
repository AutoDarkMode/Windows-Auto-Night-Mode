using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Pause and Postpone management
public partial class AutoSwitchViewModel : ObservableRecipient
{
    private void LoadPauseTimer(object? sender, EventArgs e)
    {
        _isInitializing = true;

        ApiResponse reply = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.GetPostponeStatus));

        // Time-out
        if (reply.StatusCode == StatusCode.Timeout)
        {
            CurrentPauseMode = PauseMode.Off;
            CurrentPauseMinutes = null;
            UpdateInfoText();
            _isInitializing = false;
            return;
        }

        // Disabled
        if (reply.StatusCode == StatusCode.Disabled)
        {
            CurrentPauseMode = PauseMode.Off;
            CurrentPauseMinutes = null;
            PauseInfoText = "Msg_AutoSwitchDisabled".GetLocalized();
            UpdateInfoText();
            _isInitializing = false;
            return;
        }

        try
        {
            // reply.Message == "True" means: there are active delays
            if (reply.Message == "True")
            {
                bool anyNoExpiry = false;
                bool canResume = false;

                PostponeQueueDto dto = PostponeQueueDto.Deserialize(reply.Details);

                // build list
                List<string> localisedItems = dto.Items.Select(item =>
                {
                    if (item.Expiry == null)
                    {
                        anyNoExpiry = true;
                        //return "PauseMode_Once".GetLocalized();
                    }
                    if (item.IsUserClearable)
                    {
                        canResume = true;
                    }

                    item.SetCulture(new CultureInfo(
                        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride));

                    return item.GetLocalizationData().BuildLocalizedString();
                }).ToList();

                // UI update

                _dispatcherQueue.TryEnqueue(() =>
                {
                    ResumeInfoBarEnabled = anyNoExpiry && !canResume;

                    // Determine PauseMode based on the items in the queue
                    if (dto.Items.Any(i => i.Expiry == null))
                    {
                        CurrentPauseMode = PauseMode.Once;
                        CurrentPauseMinutes = null;
                    }
                    else if (dto.Items.Any(i => i.Expiry != null))
                    {
                        CurrentPauseMode = PauseMode.Timed;
                        CurrentPauseMinutes = dto.Items
                        .Where(i => i.Expiry != null)
                        .Select(i => (int)(i.Expiry!.Value - DateTime.Now).TotalMinutes)
                        .Where(minutes => minutes > 0)
                        .FirstOrDefault();
                    }
                    else
                    {
                        CurrentPauseMode = PauseMode.Off;
                        CurrentPauseMinutes = null;
                    }

                    // InfoText
                    PauseInfoText = "ActivePauses".GetLocalized() + ": " + string.Join(", ", localisedItems);
                });
            }
            else
            {
                // no delays
                _dispatcherQueue.TryEnqueue(() =>
                {
                    CurrentPauseMode = PauseMode.Off;
                    CurrentPauseMinutes = null;
                    PauseInfoText = "Msg_AutoSwitchEnabled".GetLocalized();
                    ResumeInfoBarEnabled = false;
                });
            }
        }
        catch
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                CurrentPauseMode = PauseMode.Off;
                CurrentPauseMinutes = null;
                PauseInfoText = "Msg_AutoSwitchEnabled".GetLocalized();
                ResumeInfoBarEnabled = false;
            });
        }

        _isInitializing = false;
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
        MessageHandler.Client.SendMessageAndGetReply(Command.ToggleSkipNext);
    }

    private void SendPauseTimed(int minutes)
    {
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
