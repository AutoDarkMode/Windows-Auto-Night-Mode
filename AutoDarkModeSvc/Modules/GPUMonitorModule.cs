using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class GPUMonitorModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string NoSwitch = "no_switch_pending";
        private static readonly string ThreshLow = "threshold_low";
        private static readonly string ThreshBelow = "theshold_below";
        private static readonly string ThreshHigh = "threshold_high";
        private static readonly string Frozen = "frozen";
            
        public override string TimerAffinity { get; } = TimerName.Main;
        private GlobalState Rtc { get; }
        private AdmConfigBuilder ConfigBuilder { get; }
        private bool Monitor { get; set; }
        private bool Freeze { get; set; }
        private int Counter { get; set; }

        public GPUMonitorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            Rtc = GlobalState.Instance();
            ConfigBuilder = AdmConfigBuilder.Instance();
            Monitor = false;
            Freeze = false;
        }

        public override void Fire()
        {
            Task.Run(async () =>
            {
                DateTime sunriseMonitor = ConfigBuilder.Config.Sunrise;
                DateTime sunsetMonitor = ConfigBuilder.Config.Sunset;
                if (ConfigBuilder.Config.Location.Enabled)
                {
                    LocationHandler.GetSunTimesWithOffset(ConfigBuilder, out sunriseMonitor, out sunsetMonitor);
                }

                //the time between sunrise and sunset, aka "day"
                if (Extensions.NowIsBetweenTimes(sunriseMonitor.TimeOfDay, sunsetMonitor.TimeOfDay))
                {
                    //check if theme switching should be postponed, depending on whether a sunrise or sunset is currently pending
                    var result = await CheckForPostpone(sunsetMonitor, Freeze);
                    if (result != ThreshHigh)
                    {
                        Freeze = true;
                    }
                    //disable freezing once sun time monitoring is off the grace period
                    if (!SuntimeIsWithinSpan(sunsetMonitor))
                    {
                        Freeze = false;
                    }
                }
                else
                {
                    var result = await CheckForPostpone(sunriseMonitor, Freeze);
                    if (result != ThreshHigh)
                    {
                        Freeze = true;
                    }
                    if (!SuntimeIsWithinSpan(sunriseMonitor))
                    {
                        Freeze = false;
                    }
                }
            });
        }

        private async Task<string> CheckForPostpone(DateTime time, bool freeze)
        {
            if (SuntimeIsWithinSpan(time) && !Monitor)
            {
                // if theme switching is not frozen, start GPU monitoring
                if (!freeze)
                {
                    Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minutes");
                    Monitor = true;
                }
                else
                {
                    // if theme switching is frozen, immediately disable monitoring and return a frozen state
                    Monitor = false;
                    return Frozen;
                }
            }
            else
            {
                // if monitoring is disabled, a no switch condition is reached (meaning that the suntime is not within the grace time period)
                if (!Monitor)
                {
                    return NoSwitch;
                }
            }

            if (!Rtc.PostponeSwitch)
            {
                var gpuUsage = await GetGPUUsage();
                if (gpuUsage > ConfigBuilder.Config.GPUMonitoring.Threshold)
                {
                    Logger.Info($"postponing theme switch ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                    Rtc.PostponeSwitch = true;
                    return ThreshHigh;
                }
                else
                {
                    Logger.Info($"ending GPU usage monitoring, no postpone. threshold: ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                    Monitor = false;
                    return ThreshBelow;
                }
            }
            else
            {
                var gpuUsage = await GetGPUUsage();
                if (gpuUsage <= ConfigBuilder.Config.GPUMonitoring.Threshold)
                {
                    if (Counter >= ConfigBuilder.Config.GPUMonitoring.Samples)
                    {
                        Logger.Info($"ending GPU usage monitoring, re-enabling theme switch, threshold: {gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%");
                        Rtc.PostponeSwitch = false;
                        Monitor = false;
                        return ThreshLow;
                    }
                    Counter++;
                }
                else
                {
                    Counter = 0;
                }
                return ThreshHigh;
            }
        }

        private async Task<int> GetGPUUsage()
        {
            var pcc = new PerformanceCounterCategory("GPU Engine");
            var counterNames = pcc.GetInstanceNames();
            List<PerformanceCounter> counters = new List<PerformanceCounter>();
            var counterAccu = 0f;
            foreach (string counterName in counterNames)
            {
                if (counterName.EndsWith("engtype_3D"))
                {
                    foreach (PerformanceCounter counter in pcc.GetCounters(counterName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            counters.Add(counter);
                        }
                    }
                }
            }
            counters.ForEach(c =>
            {
                counterAccu += c.NextValue();
            });
            await Task.Delay(1000);
            counters.ForEach(c =>
            {
                counterAccu += c.NextValue();
            });
            counters.Clear();
            return (int)counterAccu;
        }

        /// <summary>
        /// checks whether a time is within a grace period (within x minutes of a DateTime)
        /// </summary>
        /// <param name="time">time to be checked</param>
        /// <returns>true if it's within the span; false otherwise</returns>
        private bool SuntimeIsWithinSpan(DateTime time)
        {
            return Extensions.NowIsBetweenTimes(
                time.AddMinutes(-Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay,
                time.AddMinutes(Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay);
        }

        public override void Cleanup()
        {
            Logger.Debug($"cleanup performed for module {Name}");
            Rtc.PostponeSwitch = false;
        }
    }
}
