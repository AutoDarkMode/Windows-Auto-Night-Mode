﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;
using AutoDarkModeConfig;
using System.IO;
using Microsoft.Win32;
using AutoDarkModeSvc.Core;
using AdmProperties = AutoDarkModeConfig.Properties;
using System.Globalization;
using System.ComponentModel;
using AutoDarkModeSvc.Events;

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
        private AdmConfigBuilder Builder { get; } = AdmConfigBuilder.Instance();

        public readonly ToolStripMenuItem forceDarkMenuItem = new();

        public readonly ToolStripMenuItem forceLightMenuItem = new();
        private bool closeApp = true;

        public Service(int timerMillis)
        {
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Builder.Config.Tunable.UICulture);
            // Tray Icon Initialization
            forceDarkMenuItem.Name = "forceDark";
            forceLightMenuItem.Name = "forceLight";
            forceDarkMenuItem.Text = AdmProperties.Resources.ForceDarkTheme;
            forceLightMenuItem.Text = AdmProperties.Resources.ForceLightTheme;


            NotifyIcon = new NotifyIcon();
            InitTray();

            // Sub-Service Initialization
            MessageServer = new AsyncPipeServer(this, 5);
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

            WindowsThemeMonitor.StartThemeMonitor();
            Timers.ForEach(t => t.Start());

            // Init window handle and register hotkeys
            _ = Handle.ToInt32();
            HotkeyHandler.Service = this;
            if (Builder.Config.Hotkeys.Enabled) HotkeyHandler.RegisterAllHotkeys(Builder);

            //exit on shutdown
            NotifyIcon.Disposed += Exit;
            SystemEvents.SessionEnded += Exit;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private void InitTray()
        {
            ToolStripMenuItem exitMenuItem = new(AdmProperties.Resources.msgClose);
            ToolStripMenuItem openConfigDirItem = new(AdmProperties.Resources.TrayMenuItemOpenConfigDir);
            exitMenuItem.Click += new EventHandler(RequestExit);
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

        private void Exit(object sender, EventArgs e)
        {
            Logger.Info("exiting service");
            MessageServer.Stop();
            ConfigMonitor.Dispose();
            WindowsThemeMonitor.StopThemeMonitor();
            Timers.ForEach(t => t.Stop());
            Timers.ForEach(t => t.Dispose());
            try
            {
                if (closeApp)
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
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "could not close app before shutting down service");
            }
            Application.Exit();
        }

        public void RequestExit(object sender, EventArgs e)
        {
            if (e is ExitEventArgs exe)
            {
                closeApp = exe.CloseApp;
            }
            if (NotifyIcon != null) NotifyIcon.Dispose();
            else Exit(sender, e);
        }

        public void Restart(object sender, EventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(Extensions.ExecutionPath)
            {
                UseShellExecute = false,
                Verb = "open"
            });
            if (e is ExitEventArgs exe)
            {
                closeApp = exe.CloseApp;
            }
            RequestExit(sender, e);
        }

        public void ForceMode(object sender, EventArgs e)
        {
            GlobalState state = GlobalState.Instance();
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi.Checked)
            {
                Logger.Info("ui signal received: stop forcing specific theme");
                state.ForcedTheme = Theme.Unknown;
                ThemeManager.RequestSwitch(Builder, new(SwitchSource.Manual));
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
                if (mi.Name == "forceLight")
                {
                    Logger.Info("ui signal received: forcing light theme");
                    state.ForcedTheme = Theme.Light;
                    ThemeHandler.EnforceNoMonitorUpdates(Builder, state, Theme.Light);
                    ThemeManager.UpdateTheme(Builder.Config, Theme.Light, new(SwitchSource.Manual));
                }
                else if (mi.Name == "forceDark")
                {
                    Logger.Info("ui signal received: forcing dark theme");
                    state.ForcedTheme = Theme.Dark;
                    ThemeHandler.EnforceNoMonitorUpdates(Builder, state, Theme.Dark);
                    ThemeManager.UpdateTheme(Builder.Config, Theme.Dark, new(SwitchSource.Manual));
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
                ThemeManager.UpdateTheme(config, Theme.Dark, new(SwitchSource.Manual));
            }
            else
            {
                if (config.WindowsThemeMode.Enabled && !config.WindowsThemeMode.MonitorActiveTheme)
                    rtc.CurrentWindowsThemeName = "";
                ThemeManager.UpdateTheme(config, Theme.Light, new(SwitchSource.Manual));
            }
        }

        private void OpenConfigDir(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = AdmConfigBuilder.ConfigDir,
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

        public const int WM_HOTKEY = 0x312;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam == (IntPtr)0)
            {
                int modifiers = (int)m.LParam & 0xFFFF;
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                List<Keys> modifiersPressed = new();
                if ((modifiers & (int)HotkeyHandler.ModifierKeys.Alt) != 0) modifiersPressed.Add(Keys.Alt);
                if ((modifiers & (int)HotkeyHandler.ModifierKeys.Shift) != 0) modifiersPressed.Add(Keys.Shift);
                if ((modifiers & (int)HotkeyHandler.ModifierKeys.Control) != 0) modifiersPressed.Add(Keys.Control);
                if ((modifiers & (int)HotkeyHandler.ModifierKeys.Win) != 0) modifiersPressed.Add(Keys.LWin);

                var match = HotkeyHandler.GetRegistered(modifiersPressed, key);
                if (match == null)
                {
                    if (modifiersPressed.Contains(Keys.LWin))
                    {
                        modifiersPressed.Remove(Keys.LWin);
                        modifiersPressed.Add(Keys.RWin);
                    }
                    match = HotkeyHandler.GetRegistered(modifiersPressed, key);
                }
                if (match != null)
                {
                    try
                    {
                        match.Action();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "error in hwnd proc while processing hotkey:");
                    }
                }
            }
            base.WndProc(ref m);
        }
    }
}
