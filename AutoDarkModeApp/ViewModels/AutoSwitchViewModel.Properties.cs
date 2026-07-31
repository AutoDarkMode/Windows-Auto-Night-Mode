using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Observable Properties
public partial class AutoSwitchViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool AutoThemeSwitchingEnabled { get; set; }

    [ObservableProperty]
    public partial SwitchTriggerMode SelectedTriggerMode { get; set; }

    [ObservableProperty]
    public partial string? LocationNextUpdateDateDescription { get; set; }

    [ObservableProperty]
    public partial bool IsNoLocationAccessInfoBarOpen { get; set; }

    [ObservableProperty]
    public partial string? LocationBlockText { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeLightStart { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeDarkStart { get; set; }

    [ObservableProperty]
    public partial string? TimePickHourClock { get; set; }

    [ObservableProperty]
    public partial Visibility TimePickerVisibility { get; set; }

    [ObservableProperty]
    public partial string? LatValue { get; set; }

    [ObservableProperty]
    public partial string? LonValue { get; set; }

    [ObservableProperty]
    public partial Visibility OffsetTimeSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial int OffsetTimesMinimum { get; set; }

    [ObservableProperty]
    public partial int OffsetLight { get; set; }

    [ObservableProperty]
    public partial int OffsetDark { get; set; }

    [ObservableProperty]
    public partial PauseMode CurrentPauseMode { get; set; }

    [ObservableProperty]
    public partial int? CurrentPauseMinutes { get; set; }

    [ObservableProperty]
    public partial bool ResumeInfoBarEnabled { get; set; }

    [ObservableProperty]
    public partial double CurrentLuxSliderPercentage { get; set; }

    [ObservableProperty]
    public partial double RemainingLuxSliderPercentage { get; set; } = 1000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmbientLightSensorTooltip))]
    public partial bool AmbientLightSensorAvailable { get; set; }

    public string AmbientLightSensorTooltip => AmbientLightSensorAvailable
        ? "AmbientLightSensor_ToolTip".GetLocalized()
        : "AmbientLightSensor_Unavailable_ToolTip".GetLocalized();

    [ObservableProperty]
    public partial double CurrentLuxReading { get; set; }

    [ObservableProperty]
    public partial string? CurrentLuxDescription { get; set; }

    [ObservableProperty]
    public partial string PauseInfoText { get; set; }

    [ObservableProperty]
    public partial int SelectedPauseIndex { get; set; }

    [ObservableProperty]
    public partial Visibility PauseOptionsOnceVisibility { get; set; }
}
