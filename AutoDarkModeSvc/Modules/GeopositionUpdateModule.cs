using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using AutoDarkModeSvc.Timers;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class GeopositionUpdateModule : AutoDarkModeModule
    {
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }


        /// <summary>
        /// Instantiates a new GeopositionUpdateModule.
        /// This module updates the user's geolocation and saves the updated value to the configuration
        /// </summary>
        /// <param name="name">unique name of the module</param>
        /// <param name="timerAffinity">name of the timer this module should be assigned to</param>
        public GeopositionUpdateModule(string name, string timerAffinity)
        {
            Name = name;
            TimerAffinity = timerAffinity;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }

        public override void Poll()
        {
            Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
        }
    }
}
