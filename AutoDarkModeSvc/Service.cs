using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using AutoDarkMode;
using AutoDarkModeApp;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Config;
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
        public Service(int timerMillis)
        {
            NotifyIcon = new NotifyIcon();
            InitTray();

            CommandServer = new ZeroMQServer(Command.DefaultPort, this);
            CommandServer.Start();

            ConfigMonitor = new AutoDarkModeConfigMonitor();
            ConfigMonitor.Start();

            ModuleTimer MainTimer = new ModuleTimer(timerMillis, "main", true);
            //ModuleTimer IOTimer = new ModuleTimer(TimerFrequency.IO, "io", true);
            ModuleTimer GeoposTimer = new ModuleTimer(TimerFrequency.Location, "geopos", false);

            Timers = new List<ModuleTimer>()
            {
                MainTimer, 
                //IOTimer, 
                GeoposTimer
            };

            MainTimer.RegisterModule(new ModuleWardenModule("ModuleWarden", Timers));

            Timers.ForEach(t => t.Start());
        }

        private void InitTray()
        {
            MenuItem exitMenuItem = new MenuItem("Close", new EventHandler(Exit));
            MenuItem switchMenuItem = new MenuItem("Switch theme", new EventHandler(SwitchThemeNow));

            NotifyIcon.Icon = Properties.Resources.AutoDarkModeIcon;
            NotifyIcon.Text = "Auto Dark Mode";
            NotifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            NotifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { switchMenuItem, exitMenuItem });
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
            Application.Exit();
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
                Console.WriteLine("Start App");
                Process.Start(@"AutoDarkModeApp.exe");
            }
        }
    }
}