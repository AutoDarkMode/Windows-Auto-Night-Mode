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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using Windows.Devices.Sensors;
using Windows.Media.Capture.Frames;

namespace AutoDarkModeSvc.Governors;

public class AmbientLightGovernor : IAutoDarkModeGovernor
{
    /// <summary>
    /// Delay in milliseconds before applying a sensor-triggered theme change.
    /// This prevents accidental switching when briefly covering the sensor.
    /// </summary>
    private const int SensorDebounceDelayMs = 1000;

    /// <summary>
    /// Tolerance in milliseconds for the debounce timer safety check.
    /// </summary>
    private const int DebounceToleranceMs = 1000;

    /// <summary>
    /// Each webcam sample turns the camera on for a few seconds, so it is not sampled more often.
    /// </summary>
    private const int MinimumWebcamIntervalSec = 10;

    public Governor Type => Governor.AmbientLight;
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private LightSensor _sensor;
    private GlobalState state = GlobalState.Instance();
    private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    private bool init = true;
    private Theme currentTheme = Theme.Unknown;
    private IAutoDarkModeModule Master { get; }
    private Theme _pendingTheme = Theme.Unknown;
    private int _debounceDelayMs;
    private readonly object _debounceLock = new();
    private AmbientLightSource _source;
    private System.Timers.Timer _sampleTimer;

    /// <summary>
    /// Set when the governor itself triggers a switch, consumed by the next <see cref="Run"/>.
    /// Sensor driven switches have no predictable switch time, so the timer cannot report a switch window
    /// ahead of them. Instead the window is reported on the fire the governor caused.
    /// </summary>
    private bool _instantSwitchWindowPending;

    public AmbientLightGovernor(IAutoDarkModeModule master)
    {
        Master = master;
    }

    public GovernorEventArgs Run()
    {
        // Reload config to pick up any changes made directly to the config file
        builder.Load();

        // Re-hook if the user switched to another light source
        if (builder.Config.AmbientLight.Source != _source)
        {
            DisableHook();
            EnableHook();
        }

        // Setting the interval restarts the timer, so only set it when it was changed
        // (local copy, DisableHook may clear the field meanwhile)
        System.Timers.Timer sampleTimer = _sampleTimer;
        double sampleInterval = GetSampleInterval();
        bool sampleIntervalChanged = sampleTimer != null && sampleTimer.Interval != sampleInterval;
        if (sampleIntervalChanged)
        {
            sampleTimer.Interval = sampleInterval;
        }

        // Re-evaluate current lux against thresholds (handles config changes)
        if (_sensor != null)
        {
            try
            {
                var reading = _sensor.GetCurrentReading();
                if (reading != null)
                {
                    EvaluateLuxReading(reading.IlluminanceInLux);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "could not get current light sensor reading:");
            }
        }

        if (init)
        {
            init = false;
        }

        // The governor caused this run, so the switch is imminent. Report a switch window with no duration,
        // which makes the governor module run the dependency modules (process block list, gpu monitor)
        // before the switch is requested, giving them the chance to postpone it
        if (_instantSwitchWindowPending)
        {
            _instantSwitchWindowPending = false;
            if (state.SwitchApproach.DependenciesPresent)
            {
                return new(true, new(SwitchSource.AmbientLightSensorModule, state.AmbientLight.Requested), instantSwitchWindow: true);
            }
        }

        // If we're in a debounce period, don't trigger any switch from the timer
        // The actual switch will happen when ApplyThemeChange calls Master.Fire
        if (_pendingTheme != Theme.Unknown)
        {
            return new(false, null);
        }

        return new(false, new(SwitchSource.AmbientLightSensorModule, state.AmbientLight.Requested));
    }

    private void EvaluateLuxReading(double lux)
    {
        // Store the last reading for re-evaluation on config change
        state.AmbientLight.LastLuxReading = lux;

        // Don't process if auto theme switching is disabled
        if (!builder.Config.AutoThemeSwitchingEnabled)
        {
            CancelPendingThemeChange();
            return;
        }

        // Don't process while a theme is forced, the request would be overridden by ThemeManager anyway
        if (state.ForcedTheme != Theme.Unknown)
        {
            CancelPendingThemeChange();
            return;
        }

        // Don't process while postpone is active (user delayed the switch)
        if (state.PostponeManager.IsPostponed)
        {
            return;
        }

        double darkThreshold = builder.Config.AmbientLight.DarkThreshold;
        double lightThreshold = builder.Config.AmbientLight.LightThreshold;

        Theme newTheme = currentTheme;

        // Hysteresis logic: only switch when crossing the appropriate threshold
        if (currentTheme != Theme.Dark && lux <= darkThreshold)
        {
            newTheme = Theme.Dark;
        }
        else if (currentTheme != Theme.Light && lux >= lightThreshold)
        {
            newTheme = Theme.Light;
        }

        if (newTheme != currentTheme && newTheme != Theme.Unknown)
        {
            // Use configured delay with 1-second minimum floor for safety
            _debounceDelayMs = Math.Max(1000, builder.Config.AmbientLight.DebounceDelayMs);
            ScheduleThemeChange(newTheme, lux, darkThreshold, lightThreshold);
        }
        else if (newTheme == currentTheme)
        {
            CancelPendingThemeChange();
        }
    }

    private CancellationTokenSource _debounceCts;
    private DateTime _debounceStartTime;

    private async void ScheduleThemeChange(Theme newTheme, double lux, double darkThreshold, double lightThreshold)
    {
        // Don't use lock with async methods
        if (_pendingTheme == newTheme && _debounceCts != null)
        {
            // Already pending for this theme
            return;
        }

        // Cancel previous
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        _pendingTheme = newTheme;
        _debounceStartTime = DateTime.Now;

        string thresholdInfo = newTheme == Theme.Light ? $"{lux:F1} lux >= {lightThreshold} lux" : $"{lux:F1} lux <= {darkThreshold} lux";

        Logger.Info($"ambient light threshold crossed ({thresholdInfo}), scheduling switch to {newTheme} mode in {_debounceDelayMs / 1000} seconds");

        try
        {
            await Task.Delay(_debounceDelayMs, _debounceCts.Token);

            // If we get here, task wasn't cancelled
            ApplyThemeChange(newTheme, lux, thresholdInfo);
        }
        catch (TaskCanceledException)
        {
            Logger.Debug($"theme change to {newTheme} cancelled (timer reset)");
        }
    }

    private void CancelPendingThemeChange()
    {
        if (_pendingTheme != Theme.Unknown && _debounceCts != null)
        {
            Logger.Debug($"cancelling pending theme change to {_pendingTheme} (lux returned to current theme range)");
            _debounceCts.Cancel();
            _debounceCts = null;
            _pendingTheme = Theme.Unknown;
        }
    }

    private void ApplyThemeChange(Theme newTheme, double lux, string thresholdInfo)
    {
        // Double check duration to prevent early firing
        if ((DateTime.Now - _debounceStartTime).TotalMilliseconds < _debounceDelayMs - DebounceToleranceMs)
        {
            Logger.Warn("debounce timer fired too early, ignoring");
            return;
        }

        lock (_debounceLock)
        {
            if (_pendingTheme != newTheme)
            {
                Logger.Debug($"theme change to {newTheme} cancelled during debounce period");
                return;
            }

            Logger.Info($"applying theme change to {newTheme} mode after debounce verification ({thresholdInfo})");

            Theme previousTheme = currentTheme;
            currentTheme = newTheme;
            state.AmbientLight.Requested = newTheme;

            _pendingTheme = Theme.Unknown;
            _debounceCts = null;

            if (currentTheme != previousTheme)
            {
                _instantSwitchWindowPending = true;
                Master.Fire(this);
            }
        }
    }

    /// <summary>
    /// Re-evaluates the last known lux reading against current config thresholds.
    /// Called when config changes (e.g., slider adjustments) to trigger IMMEDIATE theme update.
    /// This bypasses the debounce since the user explicitly changed settings.
    /// </summary>
    public void ReEvaluateWithCurrentConfig()
    {
        // Force reload config as this is triggered by a config change
        builder.Load();
        if (state.AmbientLight.LastLuxReading >= 0)
        {
            Logger.Debug("re-evaluating ambient light with updated config (immediate mode)");

            // Cancel any pending debounced change - user's config change takes priority
            CancelPendingThemeChange();

            double lux = state.AmbientLight.LastLuxReading;
            double darkThreshold = builder.Config.AmbientLight.DarkThreshold;
            double lightThreshold = builder.Config.AmbientLight.LightThreshold;

            Theme newTheme = currentTheme;
            if (lux <= darkThreshold)
            {
                newTheme = Theme.Dark;
            }
            else if (lux >= lightThreshold)
            {
                newTheme = Theme.Light;
            }

            // Apply immediately without debounce - this is a user-initiated config change
            if (newTheme != currentTheme && newTheme != Theme.Unknown)
            {
                Logger.Info($"config change triggered immediate switch to {newTheme} (lux: {lux:F1}, thresholds: {darkThreshold}/{lightThreshold})");
                currentTheme = newTheme;
                state.AmbientLight.Requested = newTheme;
                _instantSwitchWindowPending = true;
                Master.Fire(this);
            }
        }
    }

    // The webcam and the weather have no change events, so they are sampled in the configured interval instead
    private double GetSampleInterval()
    {
        double interval = _source == AmbientLightSource.Webcam
            ? TimeSpan.FromSeconds(Math.Max(MinimumWebcamIntervalSec, builder.Config.AmbientLight.WebcamIntervalSec)).TotalMilliseconds
            : TimeSpan.FromMinutes(Math.Max(AmbientLightEstimator.MinimumWeatherIntervalMin, builder.Config.AmbientLight.WeatherIntervalMin)).TotalMilliseconds;
        // the timer rejects intervals above int.MaxValue milliseconds, e.g. from a hand edited config
        return Math.Min(interval, int.MaxValue);
    }

    private async void OnSampleTimer(object sender, ElapsedEventArgs e)
    {
        try
        {
            double? lux = _source == AmbientLightSource.Webcam ? await SampleWebcamAsync() : await SampleWeatherAsync();

            // The governor may have been disabled while sampling
            bool isCurrentSample = lux != null && sender == _sampleTimer;
            if (!isCurrentSample) return;
            if (!AmbientLightEstimator.IsValidLux(lux.Value))
            {
                Logger.Warn($"ignoring invalid {_source.ToString().ToLowerInvariant()} light level: {lux}");
                return;
            }
            Logger.Debug($"{_source.ToString().ToLowerInvariant()} light level: {lux:F1} lux");
            EvaluateLuxReading(lux.Value);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not evaluate ambient light sample:");
        }
    }

    private async Task<double?> SampleWebcamAsync()
    {
        try
        {
            var group = await AmbientLightEstimator.FindWebcamAsync(builder.Config.AmbientLight.WebcamId);
            if (group == null)
            {
                Logger.Warn("no webcam available on this device");
                return null;
            }

            // The camera is only turned on for this one frame
            using var webcam = await AmbientLightEstimator.OpenWebcamAsync(group);
            MediaFrameReaderStartStatus status = await webcam.Reader.StartAsync();
            if (status != MediaFrameReaderStartStatus.Success)
            {
                Logger.Warn("could not start webcam: {0}", status);
                return null;
            }
            await Task.Delay(AmbientLightEstimator.WebcamWarmupMs);
            using var frame = webcam.Reader.TryAcquireLatestFrame();
            return AmbientLightEstimator.EstimateWebcamLux(webcam, frame);
        }
        catch (Exception ex)
        {
            Logger.Warn("could not sample light level from webcam: {0}", ex.Message);
            return null;
        }
    }

    private async Task<double?> SampleWeatherAsync()
    {
        try
        {
            if (builder.LocationData.LastUpdate == default)
            {
                Logger.Warn("no location available for weather based ambient light");
                return null;
            }
            return await AmbientLightEstimator.FetchWeatherLuxAsync(builder.LocationData.Lat, builder.LocationData.Lon, builder.Config.AmbientLight.WeatherProvider);
        }
        catch (Exception ex)
        {
            Logger.Warn("could not sample light level from weather: {0}", ex.Message);
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void OnReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
    {
        EvaluateLuxReading(args.Reading.IlluminanceInLux);
    }

    public void EnableHook()
    {
        Logger.Info("ambient light governor selected");

        // Register callback for config change re-evaluation
        state.AmbientLight.ReEvaluateCallback = ReEvaluateWithCurrentConfig;

        _source = builder.Config.AmbientLight.Source;
        if (_source != AmbientLightSource.Sensor)
        {
            try
            {
                // There is no reading until the first sample, so start from the current system theme
                currentTheme = state.InternalTheme;
                if (currentTheme == Theme.Unknown)
                {
                    currentTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
                }
                state.AmbientLight.Requested = currentTheme;
                Logger.Info($"ambient light {_source.ToString().ToLowerInvariant()} sampling started, system theme: {currentTheme}");

                _sampleTimer = new System.Timers.Timer
                {
                    Interval = GetSampleInterval(),
                    AutoReset = true
                };
                _sampleTimer.Elapsed += OnSampleTimer;
                _sampleTimer.Start();
                OnSampleTimer(_sampleTimer, null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"could not start ambient light {_source.ToString().ToLowerInvariant()} sampling:");
            }
            return;
        }

        try
        {
            _sensor = LightSensor.GetDefault();
            if (_sensor != null)
            {
                _sensor.ReadingChanged += OnReadingChanged;

                // Get initial reading to set the starting state
                var reading = _sensor.GetCurrentReading();
                if (reading != null)
                {
                    double lux = reading.IlluminanceInLux;
                    state.AmbientLight.LastLuxReading = lux;
                    double darkThreshold = builder.Config.AmbientLight.DarkThreshold;
                    double lightThreshold = builder.Config.AmbientLight.LightThreshold;

                    // Determine what theme the sensor suggests
                    Theme sensorSuggestedTheme;
                    if (lux <= darkThreshold)
                    {
                        sensorSuggestedTheme = Theme.Dark;
                    }
                    else if (lux >= lightThreshold)
                    {
                        sensorSuggestedTheme = Theme.Light;
                    }
                    else
                    {
                        // In the hysteresis zone, default to light
                        sensorSuggestedTheme = Theme.Light;
                    }

                    // CRITICAL: Use the ACTUAL current system theme as baseline, NOT what the sensor suggests
                    // This ensures we don't immediately switch themes on startup - we respect debounce
                    currentTheme = state.InternalTheme;
                    if (currentTheme == Theme.Unknown)
                    {
                        // Fallback: read from registry if InternalTheme not yet set
                        currentTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
                    }

                    // Set Requested to match current system state
                    state.AmbientLight.Requested = currentTheme;

                    Logger.Info($"ambient light sensor initialized, current reading: {lux:F1} lux, sensor suggests: {sensorSuggestedTheme}, system theme: {currentTheme}");

                    // If the sensor suggests a different theme than the current system theme,
                    // schedule a DELAYED switch (respecting the debounce)
                    if (sensorSuggestedTheme != currentTheme)
                    {
                        _debounceDelayMs = Math.Max(SensorDebounceDelayMs, builder.Config.AmbientLight.DebounceDelayMs);
                        ScheduleThemeChange(sensorSuggestedTheme, lux, darkThreshold, lightThreshold);
                    }
                }
            }
            else
            {
                Logger.Warn("no ambient light sensor available on this device");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not initialize ambient light sensor:");
        }
    }

    public void DisableHook()
    {
        try
        {
            // Cancel any pending theme change
            _debounceCts?.Cancel();
            _debounceCts = null;
            _pendingTheme = Theme.Unknown;

            if (_sampleTimer != null)
            {
                _sampleTimer.Elapsed -= OnSampleTimer;
                _sampleTimer.Dispose();
                _sampleTimer = null;
            }

            if (_sensor != null)
            {
                _sensor.ReadingChanged -= OnReadingChanged;
                _sensor = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not dispose of ambient light sensor:");
        }
    }
}
