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
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using Windows.Devices.Sensors;

namespace AutoDarkModeSvc.Governors;

public class AmbientLightGovernor : IAutoDarkModeGovernor
{
    public Governor Type => Governor.AmbientLight;
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private LightSensor _sensor;
    private GlobalState state = GlobalState.Instance();
    private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    private bool init = true;
    private Theme currentTheme = Theme.Unknown;
    private IAutoDarkModeModule Master { get; }

    public AmbientLightGovernor(IAutoDarkModeModule master)
    {
        Master = master;
    }

    public GovernorEventArgs Run()
    {
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
        return new(false, new(SwitchSource.AmbientLightSensorModule, state.AmbientLight.Requested));
    }

    private void EvaluateLuxReading(double lux)
    {
        // Store the last reading for re-evaluation on config change
        state.AmbientLight.LastLuxReading = lux;

        // Reload config to get latest threshold values
        builder.Load();
        double darkThreshold = builder.Config.AmbientLight.DarkThreshold;
        double lightThreshold = builder.Config.AmbientLight.LightThreshold;

        Theme newTheme = currentTheme;

        // Hysteresis logic: only switch when crossing the appropriate threshold
        if (currentTheme != Theme.Dark && lux <= darkThreshold)
        {
            newTheme = Theme.Dark;
            Logger.Info($"ambient light at or below dark threshold ({lux:F1} lux <= {darkThreshold} lux), switching to dark mode");
        }
        else if (currentTheme != Theme.Light && lux >= lightThreshold)
        {
            newTheme = Theme.Light;
            Logger.Info($"ambient light at or above light threshold ({lux:F1} lux >= {lightThreshold} lux), switching to light mode");
        }

        if (newTheme != currentTheme && newTheme != Theme.Unknown)
        {
            currentTheme = newTheme;
            state.AmbientLight.Requested = newTheme;
        }
    }

    /// <summary>
    /// Re-evaluates the last known lux reading against current config thresholds.
    /// Called when config changes to trigger immediate theme update.
    /// </summary>
    public void ReEvaluateWithCurrentConfig()
    {
        if (state.AmbientLight.LastLuxReading >= 0)
        {
            Logger.Debug("re-evaluating ambient light with updated config");
            Theme previousTheme = currentTheme;
            EvaluateLuxReading(state.AmbientLight.LastLuxReading);
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void OnReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
    {
        Theme previousTheme = currentTheme;
        EvaluateLuxReading(args.Reading.IlluminanceInLux);

        // Only fire if theme actually changed
        if (currentTheme != previousTheme)
        {
            Master.Fire(this);
        }
    }

public void EnableHook()
    {
        Logger.Info("ambient light governor selected");

        // Register callback for config change re-evaluation
        state.AmbientLight.ReEvaluateCallback = ReEvaluateWithCurrentConfig;

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

                    if (lux <= darkThreshold)
                    {
                        currentTheme = Theme.Dark;
                    }
                    else if (lux >= lightThreshold)
                    {
                        currentTheme = Theme.Light;
                    }
                    else
                    {
                        // In the hysteresis zone, default to current system theme or light
                        currentTheme = Theme.Light;
                    }
                    state.AmbientLight.Requested = currentTheme;
                    Logger.Info($"ambient light sensor initialized, current reading: {lux:F1} lux, initial theme: {currentTheme}");
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
