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
        public override string TimerAffinity { get; } = TimerName.Main;
        private RuntimeConfig Rtc { get; }
        private AutoDarkModeConfigBuilder ConfigBuilder { get; }
        private bool Monitor { get; set; }

        public GPUMonitorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            Rtc = RuntimeConfig.Instance();
            ConfigBuilder = AutoDarkModeConfigBuilder.Instance();
            Monitor = false;
        }

        public override void Fire()
        {
            Task.Run(() =>
            {
                DateTime sunriseMonitor = ConfigBuilder.Config.Sunrise;
                DateTime sunsetMonitor = ConfigBuilder.Config.Sunset;
                if (ConfigBuilder.Config.Location.Enabled)
                {
                    LocationHandler.ApplySunDateOffset(ConfigBuilder.Config, out sunriseMonitor, out sunsetMonitor);
                }

                //the time bewteen sunrise and sunset, aka "day"
                if (Extensions.NowIsBetweenTimes(sunriseMonitor.TimeOfDay, sunsetMonitor.TimeOfDay))
                {
                    CheckForPostpone(sunsetMonitor);
                }
                else
                {
                    CheckForPostpone(sunriseMonitor);
                }
            });
        }

        private async void CheckForPostpone(DateTime time)
        {
            if (Extensions.NowIsBetweenTimes(
                time.AddMinutes(-Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay,
                time.AddMinutes(Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay)
                && !Monitor)
            {
                Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minutes");
                Monitor = true;
            }

            if (Rtc.PostponeSwitch == false && Monitor)
            {
                var gpuUsage = await GetGPUUsage();
                if (gpuUsage > ConfigBuilder.Config.GPUMonitoring.Threshold)
                {
                    Logger.Info($"postponing theme switch ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                    Rtc.PostponeSwitch = true;
                }
                else
                {
                    Logger.Info($"ending GPU usage monitoring, no postpone. threshold: ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                    Monitor = false;
                }
            }
            else if (Rtc.PostponeSwitch == true && Monitor)
            {
                var gpuUsage = await GetGPUUsage();
                if (gpuUsage <= ConfigBuilder.Config.GPUMonitoring.Threshold)
                {
                    Logger.Info($"ending GPU usage monitoring, re-enabling theme switch, threshold: {gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%");
                    Rtc.PostponeSwitch = false;
                    Monitor = false;
                }
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
            return (int)counterAccu;
        }

        public override void Cleanup()
        {
            Logger.Debug($"cleanup performed for module {Name}");
            Rtc.PostponeSwitch = false;
        }
    }
}
