using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using AutoDarkMode;
using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc
{
    class Service : ApplicationContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        NotifyIcon NotifyIcon { get; }
        List<ModuleTimer> Timers { get; set; }
        ICommandServer CommandServer { get;  }
        AutoDarkModeConfigMonitor ConfigMonitor { get; }
        private readonly ToolStripMenuItem forceDarkMenuItem = new ToolStripMenuItem("Force Dark Mode");
        private readonly ToolStripMenuItem forceLightMenuItem = new ToolStripMenuItem("Force Light Mode");

        public Service(int timerMillis)
        {
            NotifyIcon = new NotifyIcon();
            forceDarkMenuItem.Name = "forceDark";
            forceLightMenuItem.Name = "forceLight";
            InitTray();

            CommandServer = new ZeroMQServer(Command.DefaultPort, this);
            CommandServer.Start();

            ConfigMonitor = new AutoDarkModeConfigMonitor();
            ConfigMonitor.Start();

            ModuleTimer MainTimer = new ModuleTimer(timerMillis, "main");
            //ModuleTimer IOTimer = new ModuleTimer(TimerFrequency.IO, "io");
            ModuleTimer GeoposTimer = new ModuleTimer(TimerFrequency.Location, "geopos");

            Timers = new List<ModuleTimer>()
            {
                MainTimer, 
                //IOTimer, 
                GeoposTimer
            };

            MainTimer.RegisterModule(new ModuleWardenModule("ModuleWarden", Timers, true));

            Timers.ForEach(t => t.Start());
        }

        private void InitTray()
        {
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Close");
            exitMenuItem.Click += new EventHandler(Exit);

            forceDarkMenuItem.Click += new EventHandler(ForceMode);
            forceLightMenuItem.Click += new EventHandler(ForceMode);

            NotifyIcon.Icon = Properties.Resources.AutoDarkModeIcon;
            NotifyIcon.Text = "Auto Dark Mode";
            NotifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            NotifyIcon.ContextMenuStrip = new ContextMenuStrip();
            NotifyIcon.ContextMenuStrip.Items.Add("-");
            NotifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceDarkMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceLightMenuItem);
            NotifyIcon.Visible = true;
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

        private void ForceMode(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi.Checked)
            {
                Logger.Info("ui signal received: stop forcing specific theme");
                AutoDarkModeConfig config = AutoDarkModeConfigBuilder.Instance().Config;
                RuntimeConfig rtc = RuntimeConfig.Instance();
                rtc.ForcedTheme = Theme.Undefined;
                ThemeManager.TimedSwitch(config);
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
                AutoDarkModeConfig config = AutoDarkModeConfigBuilder.Instance().Config;
                RuntimeConfig rtc = RuntimeConfig.Instance();
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
            AutoDarkModeConfig config = AutoDarkModeConfigBuilder.Instance().Config;
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

        private void OpenApp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                using Mutex appMutex = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");
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