using AutoDarkModeConfig;
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
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();
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
            if (builder.Config.WindowsThemeMode.MonitorActiveTheme)
            {
                ThemeManager.RequestSwitch(AdmConfigBuilder.Instance(), new(SwitchSource.ExternalThemeSwitch));
            }
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
                Logger.Debug("theme monitor started");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not start active theme monitor");
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
                    Logger.Debug("theme monitor stopped");
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not stop active theme monitor");
            }

        }
    }
}
