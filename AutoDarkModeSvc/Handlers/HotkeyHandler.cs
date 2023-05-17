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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using System.Globalization;
using AutoDarkModeLib.Configs;

namespace AutoDarkModeSvc.Handlers
{
    static class HotkeyHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        public static Service Service { get; set; }
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public enum ModifierKeys
        {
            Alt = 0x1,
            Control = 0x2,
            Shift = 0x4,
            Win = 0x8
        }
        private static List<HotkeyInternal> Registered { get; } = new();


        public static void RegisterAllHotkeys(AdmConfigBuilder builder)
        {
            Logger.Debug("registering hotkeys");
            try
            {
                GlobalState state = GlobalState.Instance();

                if (builder.Config.Hotkeys.ForceDark != null) Register(builder.Config.Hotkeys.ForceDark, () =>
                {
                    Logger.Info("hotkey signal received: forcing dark theme");
                    state.ForcedTheme = Theme.Dark;
                    ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Dark);
                    ThemeManager.UpdateTheme(new(SwitchSource.Manual, Theme.Dark));
                });

                if (builder.Config.Hotkeys.ForceLight != null) Register(builder.Config.Hotkeys.ForceLight, () =>
                {
                    Logger.Info("hotkey signal received: forcing light theme");
                    state.ForcedTheme = Theme.Light;
                    ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                    ThemeManager.UpdateTheme(new(SwitchSource.Manual, Theme.Light));
                });

                if (builder.Config.Hotkeys.NoForce != null) Register(builder.Config.Hotkeys.NoForce, () =>
                {
                    Logger.Info("hotkey signal received: stop forcing specific theme");
                    state.ForcedTheme = Theme.Unknown;
                    ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                    ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                });

                if (builder.Config.Hotkeys.ToggleTheme != null) Register(builder.Config.Hotkeys.ToggleTheme, () =>
                {
                    Logger.Info("hotkey signal received: toggle theme");
                    ThemeManager.SwitchThemeAutoPauseAndNotify();
                });

                if (builder.Config.Hotkeys.ToggleAutoThemeSwitch != null) Register(builder.Config.Hotkeys.ToggleAutoThemeSwitch, () =>
                {
                    Logger.Info("hotkey signal received: toggle automatic theme switch");
                    AdmConfig old = builder.Config;
                    state.SkipConfigFileReload = true;
                    builder.Config.AutoThemeSwitchingEnabled = !builder.Config.AutoThemeSwitchingEnabled;
                    builder.Save();
                    ToastHandler.InvokeAutoSwitchToggleToast();
                });

                if (builder.Config.Hotkeys.TogglePostpone != null) Register(builder.Config.Hotkeys.TogglePostpone, () =>
                {
                    if (builder.Config.AutoThemeSwitchingEnabled)
                    {
                        if (state.PostponeManager.IsSkipNextSwitch)
                        {
                            state.PostponeManager.RemoveSkipNextSwitch();
                            ToastHandler.InvokePauseAutoSwitchToast();
                            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(o => ThemeManager.RequestSwitch(new(SwitchSource.Manual)));
                        }
                        else
                        {
                            state.PostponeManager.AddSkipNextSwitch();
                            ToastHandler.InvokePauseAutoSwitchToast();
                        }
                    }
                });
            } 
            catch (Exception ex)
            {
                Logger.Error(ex, "could not register hotkeys:");
            }
           
        }

        public static void UnregisterAllHotkeys()
        {
            Logger.Debug("removing hotkeys");
            Registered.ForEach(hk => Unregister(hk.Id));
            Registered.Clear();
        }

        public static HotkeyInternal GetRegistered(List<Keys> modifiersPressed, Keys key)
        {
            return Registered.Find(hk => Enumerable.SequenceEqual(hk.Modifiers.OrderBy(e => e), modifiersPressed.OrderBy(e => e)) && hk.Key == key);
        }

        private static void Register(string hotkeyString, Action action)
        {
            if (Service == null)
            {
                Logger.Error("service not instantiated while trying to register hotkey");
            }
            List<Keys> keys = GetKeyList(hotkeyString);
            HotkeyInternal mappedHotkey = new() { StringCode = hotkeyString, Action = action };

            keys.ForEach((k) =>
            {
                if (IsModifier(k)) mappedHotkey.Modifiers.Add(k);
                else mappedHotkey.Key = k;
            });

            if (mappedHotkey.Modifiers.Count == 0)
            {
                throw new InvalidOperationException("hotkey must contain at least one of these modifiers: Shift, Alt, Ctrl, LWin, RWin");
            }
            Registered.Add(mappedHotkey);
            uint modifiersJoined = mappedHotkey.Modifiers.Select(m =>
            {
                if (m == Keys.Shift) return ModifierKeys.Shift;
                else if (m == Keys.Control) return ModifierKeys.Control;
                else if (m == Keys.Alt) return ModifierKeys.Alt;
                return ModifierKeys.Win;
            }).Select(m => (uint)m).Aggregate((accu, cur) => accu | cur);

            //Service.Invoke(new Action(() => RegisterHotKey(Service.Handle, 0, (uint)ModifierKeys.Win | (uint)ModifierKeys.Control, (int)Keys.A)));
            Service.Invoke(new Action(() => RegisterHotKey(Service.Handle, 0, modifiersJoined, (uint)mappedHotkey.Key)));

        }

        private static void Unregister(int id)
        {
            if (Service == null)
            {
                Logger.Error("service not instantiated while trying to unregister hotkey");
            }
            HotkeyInternal mappedHotkey = Registered.Find(hk => hk.Id == id);
            if (mappedHotkey != null)
            {
                Service.Invoke(new Action(() => UnregisterHotKey(Service.Handle, mappedHotkey.Id)));
            }
        }

        private static List<Keys> GetKeyList(string hotkeyString)
        {
            string[] splitKeys = hotkeyString.Split("+");
            KeysConverter converter = new();
            List<Keys> keys = new();
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");
            foreach (string keyString in splitKeys)
            {
                Keys key = (Keys)converter.ConvertFromInvariantString(keyString);
                keys.Add(key);
            }
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(builder.Config.Tunable.UICulture);
            return keys;
        }

        private static bool IsModifier(Keys key)
        {
            if (key == Keys.Shift ||
                key == Keys.Control ||
                key == Keys.Alt ||
                key == Keys.LWin ||
                key == Keys.RWin)
            {
                return true;
            }
            return false;
        }
    }

    public class HotkeyInternal
    {
        public int Id { get; set; }
        public string StringCode { get; set; }
        public List<Keys> Modifiers { get; set; } = new();
        public Keys Key { get; set; }
        public Action Action { get; set; }
    }
}
