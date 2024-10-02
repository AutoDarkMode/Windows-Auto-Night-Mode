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
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors
{
    public static class WindowsThemeMonitor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ManagementEventWatcher globalThemeEventWatcher;
        private static bool IsPaused
        {
            get;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set;
        }
        private static void HandleThemeMonitorEvent()
        {
            Logger.Debug("theme switch detected");
            Thread thread = new(() =>
            {
                try
                {
                    GlobalState.Instance().RefreshThemes(AdmConfigBuilder.Instance().Config);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not update theme name");
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while waiting for thread to stop:");
            }
            ThemeManager.RequestSwitch(new(SwitchSource.ExternalThemeSwitch, GlobalState.Instance().InternalTheme));
        }

        public static void StartThemeMonitor()
        {
            try
            {
                if (globalThemeEventWatcher != null)
                {
                    return;
                }
                globalThemeEventWatcher = WMIHandler.CreateHKCURegistryValueMonitor(HandleThemeMonitorEvent, "SOFTWARE\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Themes", "CurrentTheme");
                globalThemeEventWatcher.Start();
                Logger.Info("theme monitor enabled");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not start active theme monitor");
            }
        }

        public static void PauseThemeMonitor(TimeSpan timeSpan)
        {
            if (globalThemeEventWatcher != null && !IsPaused)
            {
                IsPaused = true;
                globalThemeEventWatcher.Stop();
                Task.Delay(timeSpan).ContinueWith(e =>
                {
                    if (globalThemeEventWatcher != null) globalThemeEventWatcher.Start();
                    IsPaused = false;
                });
            }
        }

        public static void PauseThemeMonitor()
        {
            if (globalThemeEventWatcher != null && !IsPaused)
            {
                globalThemeEventWatcher.Stop();
                IsPaused = true;
            }
        }

        public static void ResumeThemeMonitor()
        {
            if (globalThemeEventWatcher != null && IsPaused)
            {
                globalThemeEventWatcher.Start();
                IsPaused = false;
            }
        }   

        public static void StopThemeMonitor()
        {
            try
            {
                if (globalThemeEventWatcher != null)
                {
                    globalThemeEventWatcher.Stop();
                    globalThemeEventWatcher.Dispose();
                    globalThemeEventWatcher = null;
                    Logger.Info("theme monitor disabled");
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not stop active theme monitor");
            }

        }
    }
}