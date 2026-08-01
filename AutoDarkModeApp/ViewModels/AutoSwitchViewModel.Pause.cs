using System.Globalization;
using System.Diagnostics;

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
                    result.Minutes = null;
                    result.InfoText = "Statuscode: Timeout";
                    _isInitializing = false;
                    return;
                }
                // Disabled
                else if (reply.StatusCode == StatusCode.Disabled)
                {
                    result.Mode = PauseMode.Off;
                    result.Minutes = null;
                    result.InfoText = "Statuscode: Disabled";
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
                            // TODO: Use DateTime.UtcNow to avoid timezone issues, but ensure that the Expiry is also in UTC.
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

                    // Update the SelectedPauseIndex based on the current pause mode and minutes (page load)
                    int MapMinutesToIndex(int? minutes)
                    {
                        if (minutes == null) return 0; //fallback
                        int[] options = { 15, 30, 60, 120, 240, 480, 720 };
                        for (int i = 0; i < options.Length; i++)
                        {
                            if (minutes <= options[i]) return i + 2; // +2 because Off and Once are indices 0 and 1
                        }
                        return options.Length + 1; // If greater than all options, return the last index (8)
                    }

                    int desiredIndex = result.Mode switch
                    {
                        PauseMode.Off => 0,
                        PauseMode.Once => 1,
                        PauseMode.Timed => MapMinutesToIndex(result.Minutes),
                        _ => 0
                    };

                    if (SelectedPauseIndex != desiredIndex)
                    {
                        //_isInitializing = true; // Prevent triggering OnSelectedPauseIndexChanged
                        SelectedPauseIndex = desiredIndex;
                        Debug.WriteLine($"desiredIndex: {desiredIndex}, SelectedPauseIndex: {SelectedPauseIndex}");
                    }
                }
                finally
                {
                    // End initialization on UI thread after all UI properties are set
                    _isInitializing = false;
                    Debug.WriteLine($"[{DateTime.UtcNow}] CurrentPauseMinutes: {CurrentPauseMinutes}, SelectedPauseIndex: {SelectedPauseIndex}, CurrentPauseMode: {CurrentPauseMode}");
                    Debug.WriteLine($"[{DateTime.UtcNow}] PauseInfoText: {PauseInfoText}");
                }
            });
        });
    }

    partial void OnSelectedPauseIndexChanged(int value)
    {
        if (_isInitializing) return;

        switch (value)
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
        // TODO: use Task.Run for SendPauseOff, SendPauseOnce, and SendPauseTimed to avoid blocking the UI thread
        UpdateInfoText();
    }

    // TODO: create SendMessageAndGetReplyAsync with timeout/cancellation token in async, to avoid blocking the UI thread
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
