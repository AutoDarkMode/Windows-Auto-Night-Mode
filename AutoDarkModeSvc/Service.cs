using System;
using System.Diagnostics;
using System.Windows.Forms;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handler;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc
{
    class Service : ApplicationContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        NotifyIcon NotifyIcon { get; }
        ModuleTimer ModuleTimer { get; }
        ModuleTimer IOTimer { get; }
        PipeService PipeSvc { get;  }
        public Service(int timerMillis)
        { 
            NotifyIcon = new NotifyIcon();
            InitTray();

            PipeSvc = new PipeService();
            PipeSvc.Start();

            ModuleTimer = new ModuleTimer(timerMillis);
            ModuleTimer.RegisterModule(new TimeSwitchModule("TimeSwitch"));
            ModuleTimer.Start();

            IOTimer = new ModuleTimer(300000);
            IOTimer.RegisterModule(new ConfigRefreshModule("ConfigRefresh"));
            IOTimer.Start();
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

        private void Exit(object sender, EventArgs e)
        {
            PipeSvc.Stop();
            NotifyIcon.Dispose();
            ModuleTimer.Stop();
            ModuleTimer.Dispose();
            IOTimer.Stop();
            IOTimer.Dispose();
            NLog.LogManager.Shutdown();
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