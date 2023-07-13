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
        private bool Alerted { get; set; }
        private bool AllowMonitoring { get; set; } = true;
        private bool MonitoringActive { get; set; }

        public GPUMonitorModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            State = GlobalState.Instance();
            ConfigBuilder = AdmConfigBuilder.Instance();
        }

        public override Task Fire(object caller = null)
        {
            // prevents the module from activating again if it has completed its operations during the approach window
            if (!State.SwitchApproach.ThemeSwitchApproaching && !MonitoringActive)
            {
                AllowMonitoring = true;
            }
            // if a theme switch is approaching and the module is not monitoring yet, we need to enable the module
            if (State.SwitchApproach.ThemeSwitchApproaching && !MonitoringActive && AllowMonitoring)
            {
                Logger.Info($"starting GPU usage monitoring, theme switch pending");
                State.PostponeManager.Add(new(Name, isUserClearable: true));
                // monitoring can only be used again once the reset condition has been met
                AllowMonitoring = false;
                MonitoringActive = true;
            }
            // perform monitor operations
            if (MonitoringActive)
            {
                if (State.PostponeManager.Get(Name) == null)
                {
                    Logger.Info("disabling monitoring because the postpone was removed by user input");
                    MonitoringActive = false;
                    return Task.CompletedTask;
                }
                return Task.Run(async () =>
                {
                    var result = await CheckForPostpone();
                    if (result != ThreshHigh)
                    {
                        MonitoringActive = false;
                        State.PostponeManager.Remove(Name);
                    }
                });               
            }
            return Task.CompletedTask;
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
                    Counter = 0;
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

        public override void EnableHook()
        {
            State.SwitchApproach.AddDependency(this);
        }

        public override void DisableHook()
        {
            Logger.Debug($"cleanup performed for module {Name}");
            State.PostponeManager.Remove(Name);
            State.SwitchApproach.RemoveDependency(this);
        }
    }
}
