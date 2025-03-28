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
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents;

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
