using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors
{
    public static class WindowsThemeMonitor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ManagementEventWatcher globalThemeEventWatcher;
        private static void HandleThemeMonitorEvent()
        {
            Logger.Debug("theme switch detected");
            Thread thread = new(() =>
            {
                try
                {
                    GlobalState.Instance().CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
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
            ThemeManager.RequestSwitch(new(SwitchSource.ExternalThemeSwitch));
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
            if (globalThemeEventWatcher != null)
            {
                globalThemeEventWatcher.Stop();
                Task.Delay(timeSpan).ContinueWith(e =>
                {
                    if (globalThemeEventWatcher != null)
                        globalThemeEventWatcher.Start();
                });
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