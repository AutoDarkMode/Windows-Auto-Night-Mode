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

        private readonly AdmConfigBuilder configBuilder = AdmConfigBuilder.Instance();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IAutoDarkModeModule warden;

        /// <summary>
        /// Creates a new ConfigFile watcher that monitors the configuration file for changes.
        /// </summary>
        public AdmConfigMonitor()
        {
            ConfigWatcher = new FileSystemWatcher
            {
                Path = configBuilder.ConfigDir,
                Filter = Path.GetFileName(configBuilder.ConfigFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            LocationDataWatcher = new FileSystemWatcher
            {
                Path = configBuilder.ConfigDir,
                Filter = Path.GetFileName(configBuilder.LocationDataPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            ConfigWatcher.Changed += OnChangedConfig;
            LocationDataWatcher.Changed += OnChangedLocationData;
        }

        private void OnChangedConfig(object source, FileSystemEventArgs e)
        {
            if (!AdmConfigBuilder.IsFileLocked(new FileInfo(configBuilder.ConfigFilePath)))
            {
                try
                {
                    configBuilder.Load();
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
            if (!AdmConfigBuilder.IsFileLocked(new FileInfo(configBuilder.LocationDataPath)))
            {
                try
                {
                    configBuilder.LoadLocationData();
                    Logger.Debug("updated location data file");
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "location data file locked, cannot load");
                }
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
