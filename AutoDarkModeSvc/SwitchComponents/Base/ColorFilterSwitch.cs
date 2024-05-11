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
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class ColorFilterSwitch : BaseComponent<object>
    {
        private bool currentColorFilterActive;
        public ColorFilterSwitch() : base() { }
        public override bool ThemeHandlerCompatibility => true;
        protected override void EnableHook()
        {
            try
            {
                RegistryHandler.ColorFilterSetup();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed to initialize color filter");
            }

            try
            {
                currentColorFilterActive = RegistryHandler.IsColorFilterActive();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize color filter state");
            }

        }
        protected override void DisableHook()
        {
            if (!Settings.Enabled && currentColorFilterActive)
            {
                RegistryHandler.ColorFilterKeySender(false);
                currentColorFilterActive = false;
            }
        }
        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (!currentColorFilterActive && e.Theme == Theme.Dark)
            {
                return true;
            }
            else if (currentColorFilterActive && e.Theme == Theme.Light)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected override void HandleSwitch(SwitchEventArgs e)
        {
            Task.Delay(250).ContinueWith(t =>
            {
                bool oldTheme = currentColorFilterActive;
                try
                {
                    RegistryHandler.ColorFilterSetup();
                    if (e.Theme == Theme.Dark)
                    {

                        RegistryHandler.ColorFilterKeySender(true);
                        currentColorFilterActive = true;

                    }
                    else
                    {
                        RegistryHandler.ColorFilterKeySender(false);
                        currentColorFilterActive = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not enable color filter:");
                }
                Logger.Info($"update info - previous: {oldTheme}, now: {currentColorFilterActive}, enabled: {Settings.Enabled}");
            }).Wait();
        }
    }
}
