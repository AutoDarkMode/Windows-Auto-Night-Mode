using AutoDarkModeSvc.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.System.Power;

namespace AutoDarkModeSvc.Handlers
{
    class PowerHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool allowRestore = false;
        public static void DisableEnergySaver(AdmConfig config)
        {
            if (!config.Tunable.DisableEnergySaverOnThemeSwitch) 
            {
                return;
            }

            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent && PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                ChangeBatterySlider(0);
                allowRestore = true;
            }
        }

        public static void RestoreEnergySaver(AdmConfig config)
        {
            if (!config.Tunable.DisableEnergySaverOnThemeSwitch)
            {
                return;
            }

            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent && allowRestore)
            {
                ChangeBatterySlider(config.Tunable.BatterySliderDefaultValue);
                allowRestore = false;
            }
        }

        private static void ChangeBatterySlider(int value)
        {
            using Process setBatterySlider = new Process();
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

            using Process setSchemeActive = new Process();
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
