using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using Windows.Media.Capture.Frames;

namespace AutoDarkModeApp.ViewModels;

public partial class AutoSwitchViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool WebcamAvailable { get; set; }

    public int MinimumWeatherInterval => AmbientLightEstimator.MinimumWeatherIntervalMin;

    public ObservableCollection<WebcamOption> WebcamOptions { get; } = [];

    private bool _isLoadingWebcams;
    private DeviceWatcher? _webcamWatcher;

    [ObservableProperty]
    public partial string? SelectedWebcamId { get; set; }

    partial void OnSelectedWebcamIdChanged(string? value)
    {
        bool isNewUserSelection = !_isInitializing && !_isLoadingWebcams && value != null && value != _builder.Config.AmbientLight.WebcamId;
        if (!isNewUserSelection) return;

        _builder.Config.AmbientLight.WebcamId = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }

        // Restart a running preview with the selected webcam
        if (IsWebcamPreviewEnabled)
        {
            StopWebcamPreview();
            _ = StartWebcamPreviewAsync();
        }
    }

    [ObservableProperty]
    public partial AmbientLightSource SelectedAmbientLightSource { get; set; }

    partial void OnSelectedAmbientLightSourceChanged(AmbientLightSource value)
    {
        if (_isInitializing) return;

        _builder.Config.AmbientLight.Source = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }
        ShowCurrentLux();
        QueueLocationAccessInfoBarRefresh();
    }

    [ObservableProperty]
    public partial WeatherProvider SelectedWeatherProvider { get; set; }

    partial void OnSelectedWeatherProviderChanged(WeatherProvider value)
    {
        if (_isInitializing) return;

        _builder.Config.AmbientLight.WeatherProvider = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }
        ShowCurrentLux();
    }

    [ObservableProperty]
    public partial int WebcamInterval { get; set; }

    partial void OnWebcamIntervalChanged(int value)
    {
        if (!_isInitializing) RestartAmbientLightDebounce();
    }

    [ObservableProperty]
    public partial int WeatherInterval { get; set; }

    partial void OnWeatherIntervalChanged(int value)
    {
        if (!_isInitializing) RestartAmbientLightDebounce();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeatherCoordinatesVisibility))]
    public partial bool WeatherUseLocationService { get; set; }

    public Visibility WeatherCoordinatesVisibility => WeatherUseLocationService ? Visibility.Collapsed : Visibility.Visible;

    partial void OnWeatherUseLocationServiceChanged(bool value)
    {
        if (_isInitializing) return;

        _builder.Config.Location.UseGeolocatorService = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AutoSwitchViewModel");
        }
        QueueLocationAccessInfoBarRefresh();
    }

    private WebcamSession? _webcamPreview;

    // Not persisted, the webcam is only kept on while the user watches the preview
    [ObservableProperty]
    public partial bool IsWebcamPreviewEnabled { get; set; }

    private async Task LoadWebcamsAsync()
    {
        string? previousWebcamId = SelectedWebcamId;
        MediaFrameSourceGroup[] webcams;
        try
        {
            webcams = await AmbientLightEstimator.FindWebcamsAsync();
        }
        catch (Exception)
        {
            // Lists no webcam if they can't be enumerated
            webcams = [];
        }
        WebcamAvailable = webcams.Length > 0;
        WebcamOptions.Clear();
        foreach (var webcam in webcams)
        {
            WebcamOptions.Add(new WebcamOption { DisplayName = webcam.DisplayName, Id = webcam.Id });
        }

        // Show the webcam the service uses, without saving the fallback to the first webcam
        var selectedWebcam = AmbientLightEstimator.SelectWebcam(webcams, _builder.Config.AmbientLight.WebcamId);
        _isLoadingWebcams = true;
        SelectedWebcamId = selectedWebcam?.Id;
        _isLoadingWebcams = false;

        // Ambient light mode would never switch with the missing light sensor
        bool isMissingSensorSelected = !AmbientLightSensorAvailable && SelectedAmbientLightSource == AmbientLightSource.Sensor;
        if (isMissingSensorSelected)
        {
            SelectedAmbientLightSource = WebcamAvailable ? AmbientLightSource.Webcam : AmbientLightSource.Weather;
        }

        // Move a running preview to the webcam that is used now, e.g. after the previewed one was unplugged
        bool previewWebcamChanged = IsWebcamPreviewEnabled && SelectedWebcamId != previousWebcamId;
        if (!WebcamAvailable)
        {
            IsWebcamPreviewEnabled = false;
        }
        else if (previewWebcamChanged)
        {
            StopWebcamPreview();
            _ = StartWebcamPreviewAsync();
        }
    }

    public void StartWebcamMonitoring()
    {
        try
        {
            _webcamWatcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            // Removed is only raised when Added and Updated are handled as well
            _webcamWatcher.Added += WebcamWatcher_Added;
            _webcamWatcher.Removed += WebcamWatcher_Removed;
            _webcamWatcher.Updated += WebcamWatcher_Updated;
            _webcamWatcher.Start();
        }
        catch (Exception)
        {
            // The webcam list then only updates when the page is opened again
            _webcamWatcher = null;
        }
    }

    public void StopWebcamMonitoring()
    {
        try
        {
            _webcamWatcher?.Stop();
        }
        catch (InvalidOperationException)
        {
            // The watcher already stopped or aborted
        }
        _webcamWatcher = null;
    }

    private void WebcamWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        QueueWebcamsReload(sender);
    }

    private void WebcamWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        QueueWebcamsReload(sender);
    }

    private void WebcamWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // property updates don't change the webcam list
    }

    private void QueueWebcamsReload(DeviceWatcher sender)
    {
        // The initial enumeration reports the webcams LoadSettings already listed
        bool isWebcamPluggedOrUnplugged = sender == _webcamWatcher && sender.Status == DeviceWatcherStatus.EnumerationCompleted;
        if (!isWebcamPluggedOrUnplugged) return;

        _dispatcherQueue.TryEnqueue(async () => await LoadWebcamsAsync());
    }

    partial void OnIsWebcamPreviewEnabledChanged(bool value)
    {
        if (value)
        {
            _ = StartWebcamPreviewAsync();
        }
        else
        {
            StopWebcamPreview();
            SetCurrentLux(null);
        }
    }

    // Shows the light level of the selected source, the webcam only while its preview is on
    private void ShowCurrentLux()
    {
        if (_isInitializing) return;

        bool isWebcamSourceShown = SelectedTriggerMode == SwitchTriggerMode.AmbientLight && SelectedAmbientLightSource == AmbientLightSource.Webcam;
        if (!isWebcamSourceShown)
        {
            IsWebcamPreviewEnabled = false;
        }
        if (SelectedTriggerMode != SwitchTriggerMode.AmbientLight) return;

        switch (SelectedAmbientLightSource)
        {
            case AmbientLightSource.Sensor:
                SetCurrentLux(_lightSensor?.GetCurrentReading()?.IlluminanceInLux, _lightSensor == null ? AmbientLightMessage.NoSensor : AmbientLightMessage.NoReading);
                break;
            case AmbientLightSource.Webcam:
                if (!IsWebcamPreviewEnabled) SetCurrentLux(null);
                break;
            case AmbientLightSource.Weather:
                SetCurrentLux(null);
                _ = ShowWeatherLuxAsync();
                break;
        }
    }

    private async Task StartWebcamPreviewAsync()
    {
        try
        {
            var group = await AmbientLightEstimator.FindWebcamAsync(_builder.Config.AmbientLight.WebcamId);
            if (group == null)
            {
                IsWebcamPreviewEnabled = false;
                return;
            }
            var webcam = await AmbientLightEstimator.OpenWebcamAsync(group);

            // The preview may have been turned off while the webcam was opening
            bool isPreviewStillWanted = IsWebcamPreviewEnabled && _webcamPreview == null;
            if (!isPreviewStillWanted)
            {
                webcam.Dispose();
                return;
            }
            _webcamPreview = webcam;
            MediaFrameReaderStartStatus status = await webcam.Reader.StartAsync();
            if (status != MediaFrameReaderStartStatus.Success)
            {
                StopWebcamPreviewWithError(AmbientLightMessage.WebcamUnavailable);
                return;
            }
            _webcamPreviewTimer.Start();
        }
        catch (UnauthorizedAccessException)
        {
            // camera access is denied in the windows privacy settings
            StopWebcamPreviewWithError(AmbientLightMessage.WebcamAccessDenied);
        }
        catch (Exception)
        {
            // e.g. another app uses the webcam exclusively
            StopWebcamPreviewWithError(AmbientLightMessage.WebcamUnavailable);
        }
    }

    // Turns the preview off and shows why in place of the light level
    private void StopWebcamPreviewWithError(AmbientLightMessage message)
    {
        // The user may have turned the preview off meanwhile
        if (!IsWebcamPreviewEnabled) return;
        IsWebcamPreviewEnabled = false;
        SetCurrentLux(null, message);
    }

    private void StopWebcamPreview()
    {
        _webcamPreviewTimer.Stop();
        _webcamPreview?.Dispose();
        _webcamPreview = null;
    }

    private async Task ShowWeatherLuxAsync()
    {
        // The service stores the location on its first weather sample
        if (_builder.LocationData.LastUpdate == default) return;

        try
        {
            double lux = await AmbientLightEstimator.FetchWeatherLuxAsync(_builder.LocationData.Lat, _builder.LocationData.Lon, SelectedWeatherProvider);
            if (SelectedAmbientLightSource == AmbientLightSource.Weather) SetCurrentLux(lux);
        }
        catch (Exception)
        {
            // e.g. no internet connection
            if (SelectedAmbientLightSource == AmbientLightSource.Weather) SetCurrentLux(null, AmbientLightMessage.WeatherUnavailable);
        }
    }
}

public class WebcamOption
{
    public required string DisplayName { get; set; }
    public required string Id { get; set; }
}
