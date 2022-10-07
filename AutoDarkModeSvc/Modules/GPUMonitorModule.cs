using AutoDarkModeLib;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.Modules
{
    class GPUMonitorModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //private static readonly string NoSwitch = "no_switch_pending";
        private static readonly string ThreshLow = "threshold_low";
        //private static readonly string ThreshBelow = "theshold_below";
        private static readonly string ThreshHigh = "threshold_high";
        //private static readonly string Frozen = "frozen";

        public override string TimerAffinity { get; } = TimerName.Main;
        private GlobalState State { get; }
        private AdmConfigBuilder ConfigBuilder { get; }
        private int Counter { get; set; }
        private bool PostponeLight { get; set; }
        private bool PostponeDark { get; set; }
        private bool Alerted { get; set; }

        public GPUMonitorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            State = GlobalState.Instance();
            ConfigBuilder = AdmConfigBuilder.Instance();
            PostponeDark = false;
            PostponeLight = false;
        }

        public override void Fire()
        {
            Task.Run(async () =>
            {
                DateTime sunriseMonitor = ConfigBuilder.Config.Sunrise;
                DateTime sunsetMonitor = ConfigBuilder.Config.Sunset;
                if (ConfigBuilder.Config.Location.Enabled)
                {
                    LocationHandler.GetSunTimes(ConfigBuilder, out sunriseMonitor, out sunsetMonitor);
                }

                //the time between sunrise and sunset, aka "day"
                if (Helper.NowIsBetweenTimes(sunriseMonitor.TimeOfDay, sunsetMonitor.TimeOfDay))
                {
                    if (SuntimeIsWithinSpan(sunsetMonitor))
                    {
                        if (!PostponeDark)
                        {
                            Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minute(s)");
                            State.PostponeManager.Add(new(Name));
                            PostponeDark = true;
                        }
                    }
                    // if it's already light, check if the theme switch from dark to light should be delayed
                    else if (PostponeLight && DateTime.Now >= sunriseMonitor)
                    {
                        var result = await CheckForPostpone();
                        if (result != ThreshHigh)
                        {
                            PostponeLight = false;
                        }
                    }
                    else
                    {
                        if (PostponeDark || PostponeLight)
                        {
                            Logger.Info($"ending GPU usage monitoring");
                            PostponeDark = false;
                            PostponeLight = false;
                            State.PostponeManager.Remove(Name);
                        }
                    }
                }
                // the time between sunset and sunrise, aka "night"
                else
                {
                    if (SuntimeIsWithinSpan(sunriseMonitor))
                    {
                        if (!PostponeLight)
                        {
                            Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minute(s)");
                            State.PostponeManager.Add(new(Name));
                            PostponeLight = true;
                        }
                    }
                    // if it's already dark, check if the theme switch from light to dark should be delayed
                    else if (PostponeDark && DateTime.Now >= sunsetMonitor)
                    {
                        var result = await CheckForPostpone();
                        if (result != ThreshHigh)
                        {
                            PostponeDark = false;
                        }
                    }
                    else
                    {
                        if (PostponeDark || PostponeLight)
                        {
                            Logger.Info($"ending GPU usage monitoring");
                            PostponeDark = false;
                            PostponeLight = false;
                            State.PostponeManager.Remove(Name);
                        }
                    }
                }
            });
        }

        private async Task<string> CheckForPostpone()
        {
            int gpuUsage;
            try
            {
               gpuUsage = await GetGPUUsage();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not read GPU usage, re-enabling theme switch:");
                State.PostponeManager.Remove(Name);
                Alerted = false;
                return ThreshLow;
            }
            if (gpuUsage <= ConfigBuilder.Config.GPUMonitoring.Threshold)
            {
                Counter++;
                if (Counter >= ConfigBuilder.Config.GPUMonitoring.Samples)
                {
                    Logger.Info($"ending GPU usage monitoring, re-enabling theme switch, threshold: {gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%");
                    State.PostponeManager.Remove(Name);
                    Alerted = false;
                    return ThreshLow;
                }
                Logger.Debug($"lower threshold sample {Counter} ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
            }
            else
            {
                if (!Alerted)
                {
                    Logger.Info($"postponing theme switch ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                    Alerted = true;
                }
                Logger.Debug($"lower threshold sample reset ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                Counter = 0;
            }
            return ThreshHigh;
        }

        private static async Task<int> GetGPUUsage()
        {
            var pcc = new PerformanceCounterCategory("GPU Engine");
            var counterNames = pcc.GetInstanceNames();
            List<PerformanceCounter> counters = new();
            var counterAccu = 0f;
            foreach (string counterName in counterNames)
            {
                if (counterName.EndsWith("engtype_3D") || counterName.Contains("Graphics") || counterName.Contains("Copy"))
                {
                    try
                    {
                        foreach (PerformanceCounter counter in pcc.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                counters.Add(counter);
                            }
                        }
                    } 
                    catch (InvalidOperationException ex)
                    {
                        Logger.Warn(ex, "counter went away:");
                    }
                }
            }
            counters.ForEach(c =>
            {
                try
                {
                    counterAccu += c.NextValue();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "couldn't retrieve value from counter:");
                }
            });
            await Task.Delay(1000);
            counters.ForEach(c =>
            {
                try
                {
                    counterAccu += c.NextValue();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "couldn't retrieve value from counter:");
                }
            });
            counters.Clear();
            return (int)counterAccu;
        }

        /// <summary>
        /// checks whether a time is within a grace period (within x minutes before a DateTime)
        /// </summary>
        /// <param name="time">time to be checked</param>
        /// <returns>true if it's within the span; false otherwise</returns>
        private bool SuntimeIsWithinSpan(DateTime time)
        {
            return Helper.NowIsBetweenTimes(
                time.AddMinutes(-Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay,
                time.TimeOfDay);
        }

        public override void DisableHook()
        {
            Logger.Debug($"cleanup performed for module {Name}");
            State.PostponeManager.Remove(Name);
        }
    }
}
