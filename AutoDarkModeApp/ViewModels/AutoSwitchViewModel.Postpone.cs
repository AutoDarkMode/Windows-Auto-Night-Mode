using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool IsPostponed { get; set; }

    [ObservableProperty]
    public partial int SelectedPostponeIndex { get; set; }

    [ObservableProperty]
    public partial string? PostponeInfoText { get; set; }

    [ObservableProperty]
    public partial Visibility PostponeOptionsSkipOnceVisibility { get; set; }

    [ObservableProperty]
    public partial bool ResumeInfoBarEnabled { get; set; }

    private void LoadPostponeTimer(object? sender, EventArgs e)
    {
        _isInitializing = true;

        ApiResponse reply = ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.GetPostponeStatus));
        if (reply.StatusCode != StatusCode.Timeout)
        {
            if (_builder.Config.AutoThemeSwitchingEnabled)
            {
                try
                {
                    if (reply.Message == "True")
                    {
                        bool anyNoExpiry = false;
                        bool canResume = false;
                        PostponeQueueDto dto = PostponeQueueDto.Deserialize(reply.Details);
                        List<string> localizedItems = dto
                            .Items.Select(i =>
                            {
                                if (i.Expiry == null)
                                    anyNoExpiry = true;
                                if (i.IsUserClearable)
                                    canResume = true;

                                i.SetCulture(new CultureInfo(Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride));

                                return i.GetLocalizationData().BuildLocalizedString();
                            })
                            .ToList();

                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            _isInitializing = true;

                            ResumeInfoBarEnabled = anyNoExpiry && !canResume;
                            IsPostponed = canResume;
                            PostponeInfoText = "ActiveDelays".GetLocalized() + ": " + string.Join('\n', localizedItems);

                            _isInitializing = false;
                        });
                    }
                    else
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            IsPostponed = false;
                            PostponeInfoText = "ActiveDelays".GetLocalized() + ": " + "Msg_AutoSwitchEnabled".GetLocalized();
                            ResumeInfoBarEnabled = false;
                        });
                    }
                }
                catch { }
            }
        }

        _isInitializing = false;
    }

    partial void OnIsPostponedChanged(bool value)
    {
        if (_isInitializing)
            return;

        var postponeMinutes = (SelectedPostponeIndex) switch
        {
            0 => 15,
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 180,
            5 => 360,
            6 => 720,
            7 => 0,
            _ => 0,
        };

        if (postponeMinutes != 0 && value)
        {
            MessageHandler.Client.SendMessageAndGetReply($"{Command.DelayBy} {postponeMinutes}");
        }
        else if (postponeMinutes == 0 && value)
        {
            MessageHandler.Client.SendMessageAndGetReply(Command.ToggleSkipNext);
        }
        else
        {
            MessageHandler.Client.SendMessageAndGetReply(Command.ClearPostponeQueue);
            MessageHandler.Client.SendMessageAndGetReply(Command.RequestSwitch);
        }

        LoadPostponeTimer(null, new());
    }
}
