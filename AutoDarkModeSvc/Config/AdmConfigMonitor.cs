using AutoDarkModeConfig;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoDarkModeSvc.Config
{
    class AdmConfigMonitor
    {
        private FileSystemWatcher ConfigWatcher { get;  }
        private FileSystemWatcher LocationDataWatcher { get; }
        private readonly ComponentManager componentManager = ComponentManager.Instance();
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IAutoDarkModeModule warden;

        /// <summary>
        /// Creates a new ConfigFile watcher that monitors the configuration file for changes.
        /// </summary>
        public AdmConfigMonitor()
        {
            ConfigWatcher = new FileSystemWatcher
            {
                Path = builder.ConfigDir,
                Filter = Path.GetFileName(builder.ConfigFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            LocationDataWatcher = new FileSystemWatcher
            {
                Path = builder.ConfigDir,
                Filter = Path.GetFileName(builder.LocationDataPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            ConfigWatcher.Changed += OnChangedConfig;
            LocationDataWatcher.Changed += OnChangedLocationData;
        }

        private void OnChangedConfig(object source, FileSystemEventArgs e)
        {
            if (!AdmConfigBuilder.IsFileLocked(new FileInfo(builder.ConfigFilePath)))
            {
                try
                {
                    builder.Load();
                    componentManager.UpdateSettings();
                    UpdateEventStates();
                    if (warden != null)
                    {
                        warden.Fire();
                    }
                    Logger.Debug("updated configuration file");
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "config file locked, cannot load");
                }
            }
        }

        private void OnChangedLocationData(object source, FileSystemEventArgs e)
        {
            if (!AdmConfigBuilder.IsFileLocked(new FileInfo(builder.LocationDataPath)))
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
        }

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
