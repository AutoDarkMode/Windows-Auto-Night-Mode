using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoDarkModeSvc.Config
{
    class AutoDarkModeConfigMonitor
    {
        private FileSystemWatcher Watcher { get;  }
        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates a new ConfigFile watcher that monitors the configuration file for changes.
        /// </summary>
        public AutoDarkModeConfigMonitor()
        {
            Watcher = new FileSystemWatcher
            {
                Path = configBuilder.ConfigDir,
                Filter = Path.GetFileName(configBuilder.ConfigFilePath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            Watcher.Changed += OnChanged;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
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

        /// <summary>
        /// Starts a new Watcher that monitors changes in the configuration file and immediately updates
        /// </summary>
        public void Start()
        {

            Watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops the Watcher but keeps the instance active
        /// </summary>
        public void Stop()
        {
            Watcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Disposes of the Watcher. Removes all events. Class will have to be reinstantiated as the underlying <see cref="FileSystemWatcher"/> was disposed of
        /// </summary>
        public void Dispose()
        {
            Watcher.EnableRaisingEvents = false;
            Watcher.Changed -= OnChanged;
            Watcher.Dispose();
        }
    }
}
