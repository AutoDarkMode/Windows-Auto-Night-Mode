using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class GeopositionUpdateModule : AutoDarkModeModule
    {
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }
        public override string TimerAffinity { get; } = TimerName.Geopos;

        /// <summary>
        /// Instantiates a new GeopositionUpdateModule.
        /// This module updates the user's geolocation and saves the updated value to the configuration
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public GeopositionUpdateModule(string name)
        {
            Name = name;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }

        public override void Fire()
        {
            Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
        }
    }
}
