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
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    internal class GeopositionUpdateModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private AdmConfigBuilder ConfigBuilder { get; }
        public override string TimerAffinity { get; } = TimerName.Geopos;

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
    }
}
