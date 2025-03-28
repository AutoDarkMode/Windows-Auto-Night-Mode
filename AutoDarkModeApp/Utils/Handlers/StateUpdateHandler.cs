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
using System.Security.Principal;
using System.Timers;
using AutoDarkModeLib;

namespace AutoDarkModeApp.Utils.Handlers
{

    public static class StateUpdateHandler
    {
        public static SecurityIdentifier SID
        {
            get
            {
                var identity = WindowsIdentity.GetCurrent();
                return identity.User;
            }
        }

        static StateUpdateHandler()
        {
            PostponeRefreshTimer.Interval = 2000;
        }

        private static List<ElapsedEventHandler> delegatesTimer = new();
        private static List<FileSystemEventHandler> delegatesConfigWatcher = new();
        private static List<FileSystemEventHandler> delegatesScriptConfigWatcher = new();


        private static FileSystemWatcher ConfigWatcher
        {
            get;
        } = new FileSystemWatcher
        {
            Path = AdmConfigBuilder.ConfigDir,
            Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        private static FileSystemWatcher ScriptConfigWatcher
        {
            get;
        } = new FileSystemWatcher
        {
            Path = AdmConfigBuilder.ConfigDir,
            Filter = Path.GetFileName(AdmConfigBuilder.ScriptConfigPath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        private static System.Timers.Timer PostponeRefreshTimer { get; } = new();

        public static void ClearAllEvents()
        {
            foreach (var eh in delegatesTimer)
            {
                PostponeRefreshTimer.Elapsed -= eh;
            }
            delegatesTimer.Clear();
            foreach (var eh in delegatesConfigWatcher)
            {
                ConfigWatcher.Changed -= eh;
            }
            delegatesConfigWatcher.Clear();
            foreach (var eh in delegatesScriptConfigWatcher)
            {
                ScriptConfigWatcher.Changed -= eh;
            }
            delegatesScriptConfigWatcher.Clear();
        }

        public static void StartScriptWatcher()
        {
            ScriptConfigWatcher.EnableRaisingEvents = true;
        }

        public static void StopScriptWatcher()
        {
            ScriptConfigWatcher.EnableRaisingEvents = false;
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

        public static event FileSystemEventHandler OnScriptConfigUpdate
        {
            add
            {
                ScriptConfigWatcher.Changed += value;
                delegatesScriptConfigWatcher.Add(value);
            }
            remove
            {
                ScriptConfigWatcher.Changed -= value;
                delegatesScriptConfigWatcher.Remove(value);
            }
        }

        public static event FileSystemEventHandler OnConfigUpdate
        {
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
