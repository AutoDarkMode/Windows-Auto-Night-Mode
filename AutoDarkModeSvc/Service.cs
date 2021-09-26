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
using System.ComponentModel;
using AutoDarkModeConfig;

namespace AutoDarkModeSvc
{
    class Service : Form
    {
        private readonly bool allowshowdisplay = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private NotifyIcon NotifyIcon { get; }
        private List<ModuleTimer> Timers { get; set; }
        private ICommandServer CommandServer { get; }
        private AdmConfigMonitor ConfigMonitor { get; }
        private AdmConfigBuilder ConfigBuilder { get; }
        public readonly ToolStripMenuItem forceDarkMenuItem = new("Force Dark Mode");
        public readonly ToolStripMenuItem forceLightMenuItem = new("Force Light Mode");
        private delegate void SafeCallDelegate(string text);

        public Service(int timerMillis)
        {
            ConfigBuilder = AdmConfigBuilder.Instance();
            forceDarkMenuItem.Name = "forceDark";
            forceLightMenuItem.Name = "forceLight";
            if (ConfigBuilder.Config.Tunable.ShowTrayIcon)
            {
                NotifyIcon = new NotifyIcon();
                InitTray();
            }
            CommandServer = new ZeroMQServer(this);
            //CommandServer = new ZeroMQServer(Command.DefaultPort, this);
            CommandServer.Start();

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

            var warden = new WardenModule("ModuleWarden", Timers, true);
            ConfigMonitor.RegisterWarden(warden);
            ConfigMonitor.UpdateEventStates();
            MainTimer.RegisterModule(warden);

            Timers.ForEach(t => t.Start());
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
            NotifyIcon.Visible = true;
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

        public void Cleanup()
        {
            CommandServer.Stop();
            ConfigMonitor.Dispose();
            Timers.ForEach(t => t.Stop());
            Timers.ForEach(t => t.Dispose());
            NLog.LogManager.Shutdown();
        }

        public void Exit(object sender, EventArgs e)
        {
            NotifyIcon.Dispose();
            Application.Exit();
        }

        public void ForceMode(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi.Checked)
            {
                Logger.Info("ui signal received: stop forcing specific theme");
                GlobalState rtc = GlobalState.Instance();
                rtc.ForcedTheme = Theme.Undefined;
                ThemeManager.TimedSwitch(ConfigBuilder);
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
                AdmConfig config = ConfigBuilder.Config;
                GlobalState rtc = GlobalState.Instance();
                if (mi.Name == "forceLight")
                {
                    Logger.Info("ui signal received: forcing light theme");
                    rtc.ForcedTheme = Theme.Light;
                    ThemeManager.SwitchTheme(config, Theme.Light);
                }
                else if (mi.Name == "forceDark")
                {
                    Logger.Info("ui signal received: forcing dark theme");
                    rtc.ForcedTheme = Theme.Dark;
                    ThemeManager.SwitchTheme(config, Theme.Dark);
                }
                mi.Checked = true;
            }
        }

        private void SwitchThemeNow(object sender, EventArgs e)
        {
            AdmConfig config = ConfigBuilder.Config;
            Logger.Info("ui signal received: switching theme");
            if (RegistryHandler.AppsUseLightTheme())
            {
                ThemeManager.SwitchTheme(config, Theme.Dark);
            }
            else
            {
                ThemeManager.SwitchTheme(config, Theme.Light);
            }
        }

        private void OpenConfigDir(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = ConfigBuilder.ConfigDir,
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
                        Process.Start(@"AutoDarkModeApp.exe");
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