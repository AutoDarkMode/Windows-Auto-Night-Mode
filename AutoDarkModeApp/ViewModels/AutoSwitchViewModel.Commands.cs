using System.Globalization;

namespace AutoDarkModeApp.ViewModels;

// Relay Commands
public partial class AutoSwitchViewModel : ObservableRecipient
{
    [RelayCommand]
    private void AutoConfigure()
    {
        if (!AmbientLightSensorAvailable) return;

        double currentLux = CurrentLuxReading;
        double dark, light;

        // Calculate gap using exponential scaling: smaller lux values get smaller gaps,
        // larger values get proportionally larger gaps (non-linear growth)
        // Examples: 10 lux → 5 gap, 41 lux → 13 gap, 100 lux → 25 gap, 1000 lux → 126 gap
        double gap = Math.Pow(Math.Max(1, currentLux), 0.7);

        // Anchor threshold based on current active theme
        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            // Light theme: current lux is "nominal light", anchor light threshold near it
            light = Math.Max(1, currentLux * 0.95);
            dark = Math.Max(1, light - gap);
        }
        else
        {
            // Dark theme: current lux is "nominal dark", anchor dark threshold near it
            dark = Math.Max(1, currentLux * 1.05);
            light = dark + gap;
        }

        // Clamp to valid range
        AmbientLightDarkThreshold = Math.Max(1, Math.Min(dark, 9998));
        AmbientLightLightThreshold = Math.Max(AmbientLightDarkThreshold + 1, Math.Min(light, 10000));

        // Save immediately as this is a deliberate action or first-time setup
        if (_ambientLightDebounceTimer != null)
        {
            _ambientLightDebounceTimer.Stop();
            _builder.Config.AmbientLight.DarkThreshold = AmbientLightDarkThreshold;
            _builder.Config.AmbientLight.LightThreshold = AmbientLightLightThreshold;
            try
            {
                _builder.Save();
                SafeApplyTheme();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
            }
        }
    }
}
