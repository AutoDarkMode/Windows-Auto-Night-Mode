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

        public override void Fire()
        {
            DateTime nextUpdate = ConfigBuilder.LocationData.LastUpdate.Add(ConfigBuilder.Config.Location.PollingCooldownTimeSpan);
            if (DateTime.Now >= nextUpdate || (ConfigBuilder.LocationData.DataSourceIsGeolocator != ConfigBuilder.Config.Location.UseGeolocatorService))
            {
                _ = Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
            }
            else
            {
                Logger.Debug($"Next location update scheduled: {nextUpdate}");
            }
        }
    }
}
