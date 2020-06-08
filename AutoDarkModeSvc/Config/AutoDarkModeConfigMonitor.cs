using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoDarkModeSvc.Config
{
    class AutoDarkModeConfigMonitor
    {
        private FileSystemWatcher ConfigWatcher { get;  }
        private FileSystemWatcher LocationDataWatcher { get; }

        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates a new ConfigFile watcher that monitors the configuration file for changes.
        /// </summary>
        public AutoDarkModeConfigMonitor()
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
            if (!AutoDarkModeConfigBuilder.IsFileLocked(new FileInfo(configBuilder.ConfigFilePath)))
            {
                try
                {
                    configBuilder.Load();
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
            if (!AutoDarkModeConfigBuilder.IsFileLocked(new FileInfo(configBuilder.LocationDataPath)))
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
    }
}
