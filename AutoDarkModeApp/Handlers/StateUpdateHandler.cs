using AutoDarkModeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AutoDarkModeApp.Handlers
{
    public static class StateUpdateHandler
    {
        static StateUpdateHandler()
        {
            PostponeRefreshTimer.Interval = 2000;
        }

        private static List<ElapsedEventHandler> delegatesTimer = new();
        private static List<FileSystemEventHandler> delegatesConfigWatcher = new();


        private static FileSystemWatcher ConfigWatcher { get; } = new FileSystemWatcher
        {
            Path = AdmConfigBuilder.ConfigDir,
            Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        private static System.Timers.Timer PostponeRefreshTimer { get; } = new();

        public static void ClearAllEvents()
        {
            foreach (ElapsedEventHandler eh in delegatesTimer)
            {
                PostponeRefreshTimer.Elapsed -= eh;
            }
            delegatesTimer.Clear();
            foreach (FileSystemEventHandler eh in delegatesConfigWatcher)
            {
                ConfigWatcher.Changed-= eh;
            }
            delegatesConfigWatcher.Clear();
        }


        public static void StartPostponeTimer()
        {
            PostponeRefreshTimer.Start();
        }

        public static void StopPostponeTimer()
        {
            PostponeRefreshTimer.Stop();
        }

        public static void StartConfigWatcher()
        {
            ConfigWatcher.EnableRaisingEvents = true;
        }

        public static void StopConfigWatcher()
        {
            ConfigWatcher.EnableRaisingEvents = false;
        }

        public static event FileSystemEventHandler OnConfigUpdate {
            add
            {
                ConfigWatcher.Changed += value;
                delegatesConfigWatcher.Add(value);
            }
            remove
            {
                ConfigWatcher.Changed -= value;
                delegatesConfigWatcher.Remove(value);
            }
        }


        public static event ElapsedEventHandler OnPostponeTimerTick
        {
            add
            {
                PostponeRefreshTimer.Elapsed += value;
                delegatesTimer.Add(value);
            }
            remove
            {
                PostponeRefreshTimer.Elapsed -= value;
                delegatesTimer.Remove(value);
            }
        }

    }
}
