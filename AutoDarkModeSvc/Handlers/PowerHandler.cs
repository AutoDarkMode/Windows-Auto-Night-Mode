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
        public static void DisableEnergySaver()
        {
            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent)
            {
                ChangeBatterySlider(0);
            }
        }

        public static void RestoreEnergySaver(AdmConfig config)
        {
            if (PowerManager.BatteryStatus != BatteryStatus.NotPresent)
            {
                ChangeBatterySlider(config.Tunable.BatterySliderDefaultValue);
            }
        }

        private static void ChangeBatterySlider(int value)
        {
            using Process setBatterySlider = new Process();
            setBatterySlider.StartInfo.FileName = "powercfg.exe";
            setBatterySlider.StartInfo.Arguments = $"/setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD {value}";
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
            }


            using Process setSchemeActive = new Process();
            setSchemeActive.StartInfo.FileName = "powercfg.exe";
            setSchemeActive.StartInfo.Arguments = "/setactive SCHEME_CURRENT";
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
            }

        }
    }
}
