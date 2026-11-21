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
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using Windows.Devices.Geolocation;

namespace AutoDarkModeSvc.Modules;

internal class GeopositionUpdateModule : AutoDarkModeModule
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private AdmConfigBuilder ConfigBuilder { get; }
    public override string TimerAffinity { get; } = TimerName.Geopos;
    private Geolocator _geolocator;

    /// <summary>
    /// Milliseconds between position reports while watching for location changes, one hour like GNOME's night light
    /// </summary>
    private const uint PositionReportIntervalMs = 60 * 60 * 1000;

    /// <summary>
    /// Instantiates a new GeopositionUpdateModule.
    /// This module updates the user's geolocation and saves the updated value to the configuration
    /// </summary>
    /// <param name="name">unique name of the module</param>
    public GeopositionUpdateModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
    {
        ConfigBuilder = AdmConfigBuilder.Instance();
    }

    public override Task Fire(object caller = null)
    {
        UpdatePositionWatcher();
        DateTime nextUpdate = ConfigBuilder.LocationData.LastUpdate.Add(ConfigBuilder.Config.Location.PollingCooldownTimeSpan);
        if (DateTime.Now >= nextUpdate || (ConfigBuilder.LocationData.DataSourceIsGeolocator != ConfigBuilder.Config.Location.UseGeolocatorService))
        {
            return Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
        }
        else
        {
            Logger.Debug($"Next location update scheduled: {nextUpdate}");
        }
        return Task.CompletedTask;
    }

    public override void EnableHook()
    {
        UpdatePositionWatcher();
        base.EnableHook();
    }

    public override void DisableHook()
    {
        StopPositionWatcher();
        base.DisableHook();
    }

    /// <summary>
    /// Watches for location changes only while the location service is used, so windows doesn't show the location as in use
    /// with custom coordinates. Called on enable and on every poll, which picks up a changed setting within the poll interval
    /// </summary>
    private void UpdatePositionWatcher()
    {
        bool useLocationService = ConfigBuilder.Config.Location.UseGeolocatorService;
        bool isWatcherMissing = useLocationService && _geolocator == null;
        if (isWatcherMissing)
        {
            StartPositionWatcher();
        }
        else if (!useLocationService)
        {
            StopPositionWatcher();
        }
    }

    private void StartPositionWatcher()
    {
        // Update the location when the device moved, e.g. a laptop while traveling, instead of waiting for the next poll.
        // Default accuracy uses wifi and ip based positioning, which is enough for sun times and doesn't turn on the gps
        try
        {
            _geolocator = new() { DesiredAccuracy = PositionAccuracy.Default, ReportInterval = PositionReportIntervalMs };
            _geolocator.PositionChanged += Geolocator_PositionChanged;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "could not watch for location changes:");
            _geolocator = null;
        }
    }

    private void StopPositionWatcher()
    {
        if (_geolocator == null) return;

        try
        {
            _geolocator.PositionChanged -= Geolocator_PositionChanged;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "could not stop watching for location changes:");
        }
        _geolocator = null;
    }

    private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
        // An event may still arrive after DisableHook
        bool isWatchedUpdate = sender == _geolocator && ConfigBuilder.Config.Location.UseGeolocatorService;
        if (!isWatchedUpdate) return;

        try
        {
            BasicGeoposition? position = args.Position?.Coordinate?.Point?.Position;
            if (position == null) return;

            // Only update when the move shifts sunrise or sunset by more than a minute, like GNOME's night light
            DateTime today = DateTime.Today;
            Sunriset.SunriseSunset(today.Year, today.Month, today.Day, position.Value.Latitude, position.Value.Longitude, out double sunrise, out double sunset);
            Sunriset.SunriseSunset(today.Year, today.Month, today.Day, ConfigBuilder.LocationData.Lat, ConfigBuilder.LocationData.Lon, out double storedSunrise, out double storedSunset);
            bool sunTimesShifted = Math.Abs(sunrise - storedSunrise) > 1.0 / 60 || Math.Abs(sunset - storedSunset) > 1.0 / 60;
            if (!sunTimesShifted) return;

            Logger.Info("device location changed, updating geoposition");
            await Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "could not update geoposition after location change:");
        }
    }
}
