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
using AutoDarkModeLib.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.System.Power;

namespace AutoDarkModeSvc.Handlers
{
    static class PowerHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static bool EnergySaverMitigationActive { get; private set; }
        public static void RequestDisableEnergySaver(AdmConfig config)
        {
            if (EnergySaverMitigationActive) return;
            if (!config.Tunable.DisableEnergySaverOnThemeSwitch)
            {
                return;
            }

            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent && PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                ChangeBatterySlider(0);
                EnergySaverMitigationActive = true;
            }
        }

        public static void RequestRestoreEnergySaver(AdmConfig config)
        {
            if (!config.Tunable.DisableEnergySaverOnThemeSwitch)
            {
                return;
            }

            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent && EnergySaverMitigationActive)
            {
                ChangeBatterySlider(config.Tunable.BatterySliderDefaultValue);
                EnergySaverMitigationActive = false;
            }
        }

        private static void ChangeBatterySlider(int value)
        {
            using Process setBatterySlider = new();
            setBatterySlider.StartInfo.FileName = "powercfg.exe";
            setBatterySlider.StartInfo.Arguments = $"/setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD {value}";
            setBatterySlider.StartInfo.UseShellExecute = false;
            setBatterySlider.StartInfo.CreateNoWindow = true;
            try
            {
                setBatterySlider.Start();
                setBatterySlider.WaitForExit(1000);
                if (!setBatterySlider.HasExited)
                {
                    Logger.Error("had to murder powercfg /setdcvalueindex process");
                    setBatterySlider.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error modifying energy saver slider:");
                return;
            }
            Logger.Debug($"set battery saver slider to {value}");

            using Process setSchemeActive = new();
            setSchemeActive.StartInfo.FileName = "powercfg.exe";
            setSchemeActive.StartInfo.Arguments = "/setactive SCHEME_CURRENT";
            setSchemeActive.StartInfo.UseShellExecute = false;
            setSchemeActive.StartInfo.CreateNoWindow = true;
            try
            {
                setSchemeActive.Start();
                setSchemeActive.WaitForExit(1000);
                if (!setBatterySlider.HasExited)
                {
                    Logger.Error("had to murder powercfg /setactive");
                    setSchemeActive.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error modifying active power scheme state:");
                return;
            }
            Logger.Debug($"updated active power scheme");

        }
    }
}
