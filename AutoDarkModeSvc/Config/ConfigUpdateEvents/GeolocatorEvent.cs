using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Config.ConfigUpdateEvents
{
    public class GeolocatorEvent : ConfigUpdateEvent<AdmConfig>
    {
        protected override void ChangeEvent()
        {
            bool geolocatorToggled = newConfig.Location.UseGeolocatorService != oldConfig.Location.UseGeolocatorService;
            bool latChanged = newConfig.Location.CustomLat != oldConfig.Location.CustomLat;
            bool lonChanged = newConfig.Location.CustomLon != oldConfig.Location.CustomLon;
            // If geolocator has been toggled, updat the geoposition. Only update for disabled mode when lat or lon has changed
            if (geolocatorToggled || (!geolocatorToggled && !newConfig.Location.UseGeolocatorService && (latChanged || lonChanged)))
            {
                try
                {
                    Task.Run(async () => await LocationHandler.UpdateGeoposition(AdmConfigBuilder.Instance())).Wait();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error saving location data");
                }
            }
        }
    }
}
