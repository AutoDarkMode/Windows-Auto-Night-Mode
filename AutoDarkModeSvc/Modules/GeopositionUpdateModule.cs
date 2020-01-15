using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using AutoDarkModeSvc.Timers;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class GeopositionUpdateModule : IAutoDarkModeModule
    {
        public string Name { get; }
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }

        public string TimerAffinity { get; } = TimerName.Geopos;

        public GeopositionUpdateModule(string name)
        {
            Name = name;
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        }

        public void Poll()
        {
            Task.Run(() => LocationHandler.UpdateGeoposition(ConfigBuilder));
        }
    }
}
