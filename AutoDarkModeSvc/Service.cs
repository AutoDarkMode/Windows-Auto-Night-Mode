using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;
using AutoDarkModeConfig;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AutoDarkModeSvc
{
    class Service : Form
    {
        private readonly bool allowshowdisplay = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private NotifyIcon NotifyIcon { get; }
        private List<ModuleTimer> Timers { get; set; }
        private IMessageServer MessageServer { get; }
        private AdmConfigMonitor ConfigMonitor { get; }
        private AdmConfigBuilder Builder { get; }
        public readonly ToolStripMenuItem forceDarkMenuItem = new("Force Dark Mode");
        public readonly ToolStripMenuItem forceLightMenuItem = new("Force Light Mode");
        private delegate void SafeCallDelegate(string text);

        public Service(int timerMillis)
        {
            Builder = AdmConfigBuilder.Instance();
            forceDarkMenuItem.Name = "forceDark";
            forceLightMenuItem.Name = "forceLight";
            NotifyIcon = new NotifyIcon();
            InitTray();
            MessageServer = new AsyncPipeServer(this, 5);
            //CommandServer = new ZeroMQServer(Command.DefaultPort, this);
            MessageServer.Start();

            ConfigMonitor = new AdmConfigMonitor();
            ConfigMonitor.Start();

            ModuleTimer MainTimer = new(timerMillis, TimerName.Main);
            ModuleTimer IOTimer = new(TimerFrequency.IO, TimerName.IO);
            ModuleTimer GeoposTimer = new(TimerFrequency.Location, TimerName.Geopos);
            ModuleTimer StateUpdateTimer = new(TimerFrequency.StateUpdate, TimerName.StateUpdate);

            Timers = new List<ModuleTimer>()
            {
                MainTimer,
                IOTimer,
                GeoposTimer,
                StateUpdateTimer
            };

            WardenModule warden = new("ModuleWarden", Timers, true);
            ConfigMonitor.RegisterWarden(warden);
            ConfigMonitor.UpdateEventStates();
            MainTimer.RegisterModule(warden);

            Timers.ForEach(t => t.Start());

            //exit on shutdown
            SystemEvents.SessionEnded += OnExit;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private void InitTray()
        {
            ToolStripMenuItem exitMenuItem = new("Close");
            ToolStripMenuItem openConfigDirItem = new("Open Config Directory");
            exitMenuItem.Click += new EventHandler(Exit);
            openConfigDirItem.Click += new EventHandler(OpenConfigDir);
            forceDarkMenuItem.Click += new EventHandler(ForceMode);
            forceLightMenuItem.Click += new EventHandler(ForceMode);

            NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconTray;
            NotifyIcon.Text = "Auto Dark Mode";
            NotifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            NotifyIcon.ContextMenuStrip = new ContextMenuStrip();
            NotifyIcon.ContextMenuStrip.Opened += UpdateCheckboxes;
            NotifyIcon.ContextMenuStrip.Items.Add(openConfigDirItem);
            NotifyIcon.ContextMenuStrip.Items.Add("-");
            NotifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceDarkMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceLightMenuItem);

            if (Builder.Config.Tunable.ShowTrayIcon)
            {
                NotifyIcon.Visible = true;
            }
        }

        private void UpdateCheckboxes(object sender, EventArgs e)
        {
            GlobalState rtc = GlobalState.Instance();
            if (rtc.ForcedTheme == Theme.Light)
            {
                forceDarkMenuItem.Checked = false;
                forceLightMenuItem.Checked = true;
            }
            else if (rtc.ForcedTheme == Theme.Dark)
            {
                forceDarkMenuItem.Checked = true;
                forceLightMenuItem.Checked = false;
            }
            else
            {
                forceDarkMenuItem.Checked = false;
                forceLightMenuItem.Checked = false;
            }
        }

        public void OnExit(object sender, EventArgs e)
        {
            Logger.Info("exiting service");
            MessageServer.Stop();
            ConfigMonitor.Dispose();
            Timers.ForEach(t => t.Stop());
            Timers.ForEach(t => t.Dispose());
            try
            {
                Process[] pApp = Process.GetProcessesByName("AutoDarkModeApp");
                if (pApp.Length != 0)
                {
                    pApp[0].Kill();
                }
                foreach (Process p in pApp)
                {
                    p.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not close app before shutting down service");
            }
        }

        public void Exit(object sender, EventArgs e)
        {
            if (NotifyIcon != null) NotifyIcon.Dispose();
            OnExit(sender, e);
            Application.Exit();
        }

        public void ForceMode(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi.Checked)
            {
                Logger.Info("ui signal received: stop forcing specific theme");
                GlobalState rtc = GlobalState.Instance();
                rtc.ForcedTheme = Theme.Unknown;
                ThemeManager.TimedSwitch(Builder);
                mi.Checked = false;
            }
            else
            {
                foreach (var item in NotifyIcon.ContextMenuStrip.Items)
                {
                    if (item is ToolStripMenuItem)
                    {
                        (item as ToolStripMenuItem).Checked = false;
                    }
                }
                GlobalState state = GlobalState.Instance();
                if (mi.Name == "forceLight")
                {
                    Logger.Info("ui signal received: forcing light theme");
                    state.ForcedTheme = Theme.Light;
                    ThemeHandler.EnforceNoMonitorUpdates(Builder, state, Theme.Light);
                    ThemeManager.SwitchTheme(Builder.Config, Theme.Light);
                }
                else if (mi.Name == "forceDark")
                {
                    Logger.Info("ui signal received: forcing dark theme");
                    state.ForcedTheme = Theme.Dark;
                    ThemeHandler.EnforceNoMonitorUpdates(Builder, state, Theme.Dark);
                    ThemeManager.SwitchTheme(Builder.Config, Theme.Dark);
                }
                mi.Checked = true;
            }
        }

        private void SwitchThemeNow(object sender, EventArgs e)
        {
            GlobalState rtc = GlobalState.Instance();
            AdmConfig config = Builder.Config;
            Logger.Info("ui signal received: switching theme");
            if (RegistryHandler.AppsUseLightTheme())
            {
                if (config.WindowsThemeMode.Enabled && !config.WindowsThemeMode.MonitorActiveTheme)
                    rtc.CurrentWindowsThemeName = "";
                ThemeManager.SwitchTheme(config, Theme.Dark);
            }
            else
            {
                if (config.WindowsThemeMode.Enabled && !config.WindowsThemeMode.MonitorActiveTheme)
                    rtc.CurrentWindowsThemeName = "";
                ThemeManager.SwitchTheme(config, Theme.Light);
            }
        }

        private void OpenConfigDir(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = Builder.ConfigDir,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

        private void OpenApp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                using Mutex appMutex = new(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
                try
                {
                    if (e.Button == MouseButtons.Left && appMutex.WaitOne(TimeSpan.FromSeconds(2), false))
                    {
                        Console.WriteLine("Start App");
                        using Process app = new();
                        app.StartInfo.UseShellExecute = false;
                        app.StartInfo.FileName = Path.Combine(Extensions.ExecutionDir, "AutoDarkModeApp.exe");
                        app.Start();
                        appMutex.ReleaseMutex();
                    }
                }
                catch (AbandonedMutexException ex)
                {
                    Logger.Debug(ex, "mutex abandoned before wait");
                }
            }
        }
    }
}