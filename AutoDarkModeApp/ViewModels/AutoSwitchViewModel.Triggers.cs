using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Trigger mode handling
public partial class AutoSwitchViewModel : ObservableRecipient
{
    [RelayCommand]
    private void SetTriggerMode(string mode)
    {
        if (Enum.TryParse<SwitchTriggerMode>(mode, out var result))
        {
            SelectedTriggerMode = result;
        }
    }

    partial void OnSelectedTriggerModeChanged(SwitchTriggerMode value)
    {
        if (_isInitializing)
            return;

        // Each case fully controls all visibility states to prevent flickering
        switch (value)
        {
            case SwitchTriggerMode.CustomTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                break;

            case SwitchTriggerMode.LocationTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = true;
                TimePickerVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case SwitchTriggerMode.CoordinateTimes:
                _builder.Config.Governor = Governor.Default;
                _builder.Config.Location.Enabled = true;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Visible;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = -720;
                break;

            case SwitchTriggerMode.WindowsNightLight:
                _builder.Config.Governor = Governor.NightLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Visible;
                OffsetTimesMinimum = 0;
                break;

            case SwitchTriggerMode.AmbientLight:
                // Run auto-configure only if we are switching to Ambient Light and values are still defaults
                // This prevents overwriting user's custom settings when switching modes
                if (_builder.Config.AmbientLight.DarkThreshold == 40 && _builder.Config.AmbientLight.LightThreshold == 80)
                {
                    AutoConfigureCommand.Execute(null);
                }

                _builder.Config.Governor = Governor.AmbientLight;
                _builder.Config.AutoThemeSwitchingEnabled = true;
                _builder.Config.Location.Enabled = false;
                _builder.Config.Location.UseGeolocatorService = false;
                TimePickerVisibility = Visibility.Collapsed;
                OffsetTimeSettingsCardVisibility = Visibility.Collapsed;
                PauseOptionsOnceVisibility = Visibility.Collapsed;

                if (SelectedPauseIndex == 1) // Once
                    SelectedPauseIndex = 0; // Off
                break;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        SafeApplyTheme();
    }
}
