using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;
using AutoDarkModeLib;
using System.IO;
using Microsoft.Win32;
using AutoDarkModeSvc.Core;
using AdmProperties = AutoDarkModeLib.Properties;
using System.Globalization;
using System.ComponentModel;
using AutoDarkModeSvc.Events;
using System.Drawing;
using System.Drawing.Drawing2D;
using static AutoDarkModeSvc.DarkColorTable;

namespace AutoDarkModeSvc
{
    class Service : Form
    {
        private readonly bool allowshowdisplay = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private NotifyIcon NotifyIcon { get; }
        private List<ModuleTimer> Timers { get; set; }
        private IMessageServer MessageServer { get; }
        private AdmConfigMonitor ConfigMonitor { get; }
        private AdmConfigBuilder Builder { get; } = AdmConfigBuilder.Instance();
        GlobalState state = GlobalState.Instance();

        public readonly ToolStripMenuItem forceDarkMenuItem = new();
        public readonly ToolStripMenuItem forceLightMenuItem = new();
        public readonly ToolStripMenuItem autoThemeSwitchingItem = new();
        public readonly ToolStripMenuItem toggleThemeItem = new();
        public readonly ToolStripMenuItem pauseThemeSwitchItem = new();

        private readonly ToolStripProfessionalRenderer toolStripDarkRenderer = new DarkRenderer();
        private readonly ToolStripProfessionalRenderer toolStripDefaultRenderer = new ToolStripProfessionalRenderer();

        private bool closeApp = true;

        public Service(int timerMillis)
        {
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Builder.Config.Tunable.UICulture);
            // Tray Icon Initialization
            forceDarkMenuItem.Name = "forceDark";
            forceLightMenuItem.Name = "forceLight";
            autoThemeSwitchingItem.Name = "autoThemeSwitching";
            toggleThemeItem.Name = "toggleTheme";
            pauseThemeSwitchItem.Name = "pauseThemeSwitch";
            forceDarkMenuItem.Text = AdmProperties.Resources.ForceDarkTheme;
            forceLightMenuItem.Text = AdmProperties.Resources.ForceLightTheme;
            autoThemeSwitchingItem.Text = AdmProperties.Resources.AutomaticThemeSwitch;
            toggleThemeItem.Text = AdmProperties.Resources.ToggleTheme;

            NotifyIcon = new NotifyIcon();
            InitTray();

            // Sub-Service Initialization
            MessageServer = new AsyncPipeServer(this, 5);
            MessageServer.Start();

            ConfigMonitor = AdmConfigMonitor.Instance();
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

            if (Builder.Config.WindowsThemeMode.MonitorActiveTheme) WindowsThemeMonitor.StartThemeMonitor();
            Timers.ForEach(t => t.Start());

            // Init window handle and register hotkeys
            _ = Handle.ToInt32();
            HotkeyHandler.Service = this;
            if (Builder.Config.Hotkeys.Enabled) HotkeyHandler.RegisterAllHotkeys(Builder);

            //exit on shutdown
            NotifyIcon.Disposed += Exit;
            SystemEvents.SessionEnded += Exit;

            if (Builder.Config.AutoThemeSwitchingEnabled && Builder.Config.IdleChecker.Enabled)
            {
                ThemeManager.RequestSwitch(new(SwitchSource.Startup));
            }
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
            autoThemeSwitchingItem.Click += new EventHandler(ToggleAutoThemeSwitching);
            toggleThemeItem.Click += new EventHandler(ToggleTheme);
            pauseThemeSwitchItem.Click += new EventHandler(PauseThemeSwitch);

            NotifyIcon.Icon = Properties.Resources.AutoDarkModeIconTray;
            NotifyIcon.Text = "Auto Dark Mode";
            NotifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            NotifyIcon.ContextMenuStrip = new ContextMenuStrip();
            NotifyIcon.ContextMenuStrip.Opened += UpdateContextMenu;
            NotifyIcon.ContextMenuStrip.Items.Add(openConfigDirItem);
            NotifyIcon.ContextMenuStrip.Items.Add("-");
            NotifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceDarkMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, forceLightMenuItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, new ToolStripSeparator());
            NotifyIcon.ContextMenuStrip.Items.Insert(0, toggleThemeItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, pauseThemeSwitchItem);
            NotifyIcon.ContextMenuStrip.Items.Insert(0, autoThemeSwitchingItem);

            //NotifyIcon.ContextMenuStrip.ForeColor = Color.FromArgb(232, 232, 232);

            if (Builder.Config.Tunable.ShowTrayIcon)
            {
                NotifyIcon.Visible = true;
            }
        }

        private void UpdateContextMenu(object sender, EventArgs e)
        {
            if (state.RequestedTheme == Theme.Dark)
            {
                NotifyIcon.ContextMenuStrip.Renderer = toolStripDarkRenderer;
            }
            else
            {
                NotifyIcon.ContextMenuStrip.Renderer = toolStripDefaultRenderer;
            }

            if (state.ForcedTheme == Theme.Light)
            {
                forceDarkMenuItem.Checked = false;
                forceLightMenuItem.Checked = true;
            }
            else if (state.ForcedTheme == Theme.Dark)
            {
                forceDarkMenuItem.Checked = true;
                forceLightMenuItem.Checked = false;
            }
            else
            {
                forceDarkMenuItem.Checked = false;
                forceLightMenuItem.Checked = false;
            }
            autoThemeSwitchingItem.Checked = builder.Config.AutoThemeSwitchingEnabled;

            if (builder.Config.AutoThemeSwitchingEnabled) pauseThemeSwitchItem.Visible = true;
            else pauseThemeSwitchItem.Visible = false;

            PostponeItem tempDelay = state.PostponeManager.Get(Helper.DelaySwitchItemName);
            if (tempDelay != null && !state.PostponeManager.IsSkipNextSwitch)
            {
                DateTime expiry = tempDelay.Expiry ?? new();
                pauseThemeSwitchItem.Checked = true;
                if (expiry.Day > DateTime.Now.Day) pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.UntilTime} {expiry:ddd HH:mm})";
                else pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.UntilTime} {expiry:HH:mm})";
            }
            else
            {
                pauseThemeSwitchItem.Checked = state.PostponeManager.IsSkipNextSwitch;
                (DateTime expiry, SkipType skipType) = state.PostponeManager.GetSkipNextSwitchExpiryTime();
                if (expiry.Year != 1)
                {
                    if (expiry.Day > DateTime.Now.Day) pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.UntilTime} {expiry:ddd HH:mm})";
                    else pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.UntilTime} {expiry:HH:mm})";
                }
                else
                {
                    if (skipType == SkipType.Sunrise)
                    {
                        pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.ThemeSwitchPauseUntilSunset})";
                    }
                    else if (skipType == SkipType.Sunset)
                    {
                        pauseThemeSwitchItem.Text = $"{AdmProperties.Resources.ThemeSwitchPause} ({AdmProperties.Resources.ThemeSwitchPauseUntilSunrise})";
                    }
                    else
                    {
                        pauseThemeSwitchItem.Text = AdmProperties.Resources.ThemeSwitchPause;
                    }
                }
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
            _ = Process.Start(new ProcessStartInfo(Helper.ExecutionPath)
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

        public void PauseThemeSwitch(object sender, EventArgs e)
        {
            if (state.PostponeManager.IsSkipNextSwitch || state.PostponeManager.Get(Helper.DelaySwitchItemName) != null)
            {
                state.PostponeManager.RemoveUserClearablePostpones();
                ThemeManager.RequestSwitch(new(SwitchSource.Manual));
            }
            else
            {
                state.PostponeManager.AddSkipNextSwitch();
            }
        }

        public void ToggleTheme(object sender, EventArgs e)
        {
            Theme newTheme = ThemeManager.SwitchThemeAutoPauseAndNotify();
            Logger.Info($"ui signal received: theme toggle: switching to {Enum.GetName(typeof(Theme), newTheme).ToLower()} theme");
        }

        public void ToggleAutoThemeSwitching(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            AdmConfig old = builder.Config;

            if (mi.Checked)
            {
                Logger.Info("ui signal received: disabling auto theme switching");

                state.SkipConfigFileReload = true;
                builder.Config.AutoThemeSwitchingEnabled = false;
                AdmConfigMonitor.Instance().PerformConfigUpdate(old, internalUpdate: true);
                mi.Checked = false;
            }
            else
            {
                Logger.Info("ui signal received: enabling auto theme switching");
                state.SkipConfigFileReload = true;
                builder.Config.AutoThemeSwitchingEnabled = true;
                AdmConfigMonitor.Instance().PerformConfigUpdate(old, internalUpdate: true);
                ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                mi.Checked = true;
            }

            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not save config: ");
            }
        }

        public void ForceMode(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi.Checked)
            {
                Logger.Info("ui signal received: stop forcing specific theme");
                state.ForcedTheme = Theme.Unknown;
                ThemeManager.RequestSwitch(new(SwitchSource.Manual));
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
                    ThemeManager.UpdateTheme(Theme.Light, new(SwitchSource.Manual));
                }
                else if (mi.Name == "forceDark")
                {
                    Logger.Info("ui signal received: forcing dark theme");
                    state.ForcedTheme = Theme.Dark;
                    ThemeHandler.EnforceNoMonitorUpdates(Builder, state, Theme.Dark);
                    ThemeManager.UpdateTheme(Theme.Dark, new(SwitchSource.Manual));
                }
                mi.Checked = true;
            }
        }

        private void SwitchThemeNow(object sender, EventArgs e)
        {
            AdmConfig config = Builder.Config;
            Logger.Info("ui signal received: switching theme");
            if (RegistryHandler.AppsUseLightTheme())
            {
                if (config.WindowsThemeMode.Enabled && !config.WindowsThemeMode.MonitorActiveTheme)
                    state.CurrentWindowsThemePath = "";
                ThemeManager.UpdateTheme(Theme.Dark, new(SwitchSource.Manual));
            }
            else
            {
                if (config.WindowsThemeMode.Enabled && !config.WindowsThemeMode.MonitorActiveTheme)
                    state.CurrentWindowsThemePath = "";
                ThemeManager.UpdateTheme(Theme.Light, new(SwitchSource.Manual));
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
                    if (e.Button == MouseButtons.Left)
                    {
                        if (appMutex.WaitOne(TimeSpan.FromMilliseconds(50), false))
                        {
                            Console.WriteLine("Start App");
                            using Process app = new();
                            app.StartInfo.UseShellExecute = false;
                            app.StartInfo.FileName = Path.Combine(Helper.ExecutionDir, "AutoDarkModeApp.exe");
                            app.Start();
                            appMutex.ReleaseMutex();
                        }
                        else
                        {
                            List<Process> processes = new(Process.GetProcessesByName("AutoDarkModeApp"));
                            if (processes.Count > 0)
                            {
                                WindowHelper.BringProcessToFront(processes[0]);
                            }
                        }
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

    public static class WindowHelper
    {
        public static void BringProcessToFront(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        const int SW_RESTORE = 9;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(32, 32, 32); }
        }
        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(32, 32, 32); }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Color.FromArgb(64, 64, 64); }
        }
        public override Color MenuItemSelectedGradientEnd
        {
            get { return Color.FromArgb(64, 64, 64); }
        }
        public override Color ToolStripDropDownBackground
        {
            get {return Color.FromArgb(32, 32, 32); }
        }
        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(32, 32, 32); }
        }
        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(32, 32, 32); }
        }
        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(32, 32, 32); }
        }

        public class DarkRenderer : ToolStripProfessionalRenderer
        {
            public DarkRenderer()
                : base(new DarkColorTable())
            {
            }
            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(e.ArrowRectangle.Location, e.ArrowRectangle.Size);
                r.Inflate(-2, -6);
                e.Graphics.DrawLines(Pens.Black, new Point[]{
                    new Point(r.Left, r.Top),
                    new Point(r.Right, r.Top + r.Height /2),
                    new Point(r.Left, r.Top+ r.Height)});
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
                r.Inflate(-4, -6);
                e.Graphics.DrawLines(Pens.White, new Point[]{
                    new Point(r.Left, r.Bottom - r.Height /2),
                    new Point(r.Left + r.Width /3,  r.Bottom),
                    new Point(r.Right, r.Top)});
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (e.Item is ToolStripMenuItem)
                {
                    e.TextColor = Color.FromArgb(232, 232, 232);
                }

                base.OnRenderItemText(e);
            }

        }
    }
}
