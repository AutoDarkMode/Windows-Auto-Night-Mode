namespace AutoDarkModeApp.Models;

public enum SwitchTriggerMode
{
    CustomTimes,
    LocationTimes,
    CoordinateTimes,
    WindowsNightLight,
    AmbientLight,
};

public sealed record TriggerModeSettings(
    Governor Governor,
    bool LocationEnabled,
    bool UseGeolocatorService,
    Visibility LocationSettingsVisibility,
    Visibility CustomTimeSettingsVisibility,
    Visibility OffsetTimeSettingsVisibility,
    Visibility PostponeOptionsSkipOnceVisibility,
    int OffsetMinimum,
    bool ForcesAutoThemeSwitching
)
{
    public static TriggerModeSettings Get(SwitchTriggerMode mode)
    {
        return mode switch
        {
            SwitchTriggerMode.CustomTimes => new(
                Governor: Governor.Default,
                LocationEnabled: false,
                UseGeolocatorService: false,
                LocationSettingsVisibility: Visibility.Collapsed,
                CustomTimeSettingsVisibility: Visibility.Visible,
                OffsetTimeSettingsVisibility: Visibility.Collapsed,
                PostponeOptionsSkipOnceVisibility: Visibility.Visible,
                OffsetMinimum: -720,
                ForcesAutoThemeSwitching: false
            ),
            SwitchTriggerMode.LocationTimes => new(
                Governor: Governor.Default,
                LocationEnabled: true,
                UseGeolocatorService: true,
                LocationSettingsVisibility: Visibility.Visible,
                CustomTimeSettingsVisibility: Visibility.Visible,
                OffsetTimeSettingsVisibility: Visibility.Visible,
                PostponeOptionsSkipOnceVisibility: Visibility.Visible,
                OffsetMinimum: -720,
                ForcesAutoThemeSwitching: false
            ),
            SwitchTriggerMode.CoordinateTimes => new(
                Governor: Governor.Default,
                LocationEnabled: true,
                UseGeolocatorService: false,
                LocationSettingsVisibility: Visibility.Visible,
                CustomTimeSettingsVisibility: Visibility.Visible,
                OffsetTimeSettingsVisibility: Visibility.Visible,
                PostponeOptionsSkipOnceVisibility: Visibility.Visible,
                OffsetMinimum: -720,
                ForcesAutoThemeSwitching: false
            ),
            SwitchTriggerMode.WindowsNightLight => new(
                Governor: Governor.NightLight,
                LocationEnabled: false,
                UseGeolocatorService: false,
                LocationSettingsVisibility: Visibility.Collapsed,
                CustomTimeSettingsVisibility: Visibility.Collapsed,
                OffsetTimeSettingsVisibility: Visibility.Visible,
                PostponeOptionsSkipOnceVisibility: Visibility.Visible,
                OffsetMinimum: 0,
                ForcesAutoThemeSwitching: true
            ),
            SwitchTriggerMode.AmbientLight => new(
                Governor: Governor.AmbientLight,
                LocationEnabled: false,
                UseGeolocatorService: false,
                LocationSettingsVisibility: Visibility.Collapsed,
                CustomTimeSettingsVisibility: Visibility.Collapsed,
                OffsetTimeSettingsVisibility: Visibility.Collapsed,
                PostponeOptionsSkipOnceVisibility: Visibility.Collapsed,
                OffsetMinimum: -720,
                ForcesAutoThemeSwitching: true
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
    }
}
