﻿using AutoDarkModeConfig;
using AutoDarkModeSvc.Monitors.ConfigUpdateEvents;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors
{
    class AdmConfigMonitor
    {
        private FileSystemWatcher ConfigWatcher { get; }
        private FileSystemWatcher LocationDataWatcher { get; }
        private FileSystemWatcher ScriptConfigWatcher { get; }
        private readonly ComponentManager componentManager = ComponentManager.Instance();
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly GlobalState state = GlobalState.Instance();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IAutoDarkModeModule warden;

        /// <summary>
        /// Creates a new ConfigFile watcher that monitors the configuration file for changes.
        /// </summary>
        public AdmConfigMonitor()
        {
            ConfigWatcher = new FileSystemWatcher
            {
                Path = AdmConfigBuilder.ConfigDir,
                Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            LocationDataWatcher = new FileSystemWatcher
            {
                Path = AdmConfigBuilder.ConfigDir,
                Filter = Path.GetFileName(AdmConfigBuilder.LocationDataPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            ScriptConfigWatcher = new()
            {
                Path = AdmConfigBuilder.ConfigDir,
                Filter = Path.GetFileName(AdmConfigBuilder.ScriptConfigPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            ScriptConfigWatcher.Changed += OnChangedScriptConfig;
            ConfigWatcher.Changed += OnChangedConfig;
            LocationDataWatcher.Changed += OnChangedLocationData;
            
            IConfigUpdateEvent<AdmConfig> geolocatorEvent = new GeolocatorEvent();
            IConfigUpdateEvent<AdmConfig> themeModeEvent = new ThemeModeEvent(componentManager);
            IConfigUpdateEvent<AdmConfig> hotkeyEvent = new HotkeyEvent();

            //change event trackers
            builder.ConfigUpdatedHandler += geolocatorEvent.OnConfigUpdate;
            builder.ConfigUpdatedHandler += themeModeEvent.OnConfigUpdate;
            builder.ConfigUpdatedHandler += hotkeyEvent.OnConfigUpdate;
        }

        private void OnChangedConfig(object source, FileSystemEventArgs e)
        {
            _ = state.ConfigIsUpdatingWaitHandle.Reset();
            state.ConfigIsUpdating = true;
            if (state.SkipConfigFileReload)
            {
                state.SkipConfigFileReload = false;
                Logger.Debug("skipping config file reload, update source internal");
                return;
            }
            try
            {
                AdmConfig oldConfig = builder.Config;
                builder.Load();
                componentManager.UpdateSettings();
                UpdateEventStates();
                builder.OnConfigUpdated(oldConfig);

                // fire warden ro register/unregister enabled/disabled modules
                if (warden != null)
                {
                    warden.Fire();
                }
                Logger.Debug("updated configuration file");
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "config file load failed:");
            }
            state.ConfigIsUpdating = false;
            if (!state.ConfigIsUpdatingWaitHandle.Set()) Logger.Fatal("could not trigger reset event");
        }

        private void OnChangedLocationData(object source, FileSystemEventArgs e)
        {
            try
            {
                builder.LoadLocationData();
                Logger.Debug("updated location data file");
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "location data file locked, cannot load");
            }
        }

        private void OnChangedScriptConfig(object source, FileSystemEventArgs e)
        {
            try
            {
                builder.LoadScriptConfig();
                componentManager.UpdateScriptSettings();
                Logger.Debug("updated script config file");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not refresh script config file, custom scripts most likely will not work:");
            }
        }

        /// <summary>
        /// Registers or deregisters events based on their enabled setting
        /// </summary>
        public void UpdateEventStates()
        {
            if (builder.Config.Events.DarkThemeOnBattery)
            {
                PowerEventHandler.RegisterThemeEvent();
            }
            else
            {
                PowerEventHandler.DeregisterThemeEvent();
            }

            if (builder.Config.Events.SystemResumeTrigger)
            {
                PowerEventHandler.RegisterResumeEvent();
            }
            else
            {
                PowerEventHandler.DeregisterResumeEvent();
            }
        }


        /// <summary>
        /// Starts a new Watcher that monitors changes in the configuration file and immediately updates
        /// </summary>
        public void Start()
        {
            ConfigWatcher.EnableRaisingEvents = true;
            LocationDataWatcher.EnableRaisingEvents = true;
            ScriptConfigWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops the Watcher but keeps the instance active
        /// </summary>
        public void Stop()
        {
            ConfigWatcher.EnableRaisingEvents = false;
            LocationDataWatcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Disposes of the Watcher. Removes all events. Class will have to be reinstantiated as the underlying <see cref="FileSystemWatcher"/> was disposed of
        /// </summary>
        public void Dispose()
        {
            ConfigWatcher.EnableRaisingEvents = false;
            ConfigWatcher.Changed -= OnChangedConfig;
            ConfigWatcher.Dispose();
            LocationDataWatcher.EnableRaisingEvents = false;
            LocationDataWatcher.Changed -= OnChangedConfig;
            LocationDataWatcher.Dispose();
        }

        public void RegisterWarden(IAutoDarkModeModule warden)
        {
            this.warden = warden;
        }

        public void DeregisterWarden()
        {
            warden = null;
        }
    }
}
