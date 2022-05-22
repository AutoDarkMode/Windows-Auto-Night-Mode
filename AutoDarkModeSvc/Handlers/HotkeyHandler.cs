using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AutoDarkModeSvc.Monitors;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.Handlers
{
    static class HotkeyHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
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
                if (builder.Config.Hotkeys.ForceDarkHotkey != null) Register(builder.Config.Hotkeys.ForceDarkHotkey, () =>
                {
                    Logger.Info("hotkey signal received: forcing dark theme");
                    state.ForcedTheme = Theme.Dark;
                    ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Dark);
                    ThemeManager.UpdateTheme(builder.Config, Theme.Dark, new(SwitchSource.Manual));
                });
                if (builder.Config.Hotkeys.ForceLightHotkey != null) Register(builder.Config.Hotkeys.ForceLightHotkey, () =>
                {
                    Logger.Info("hotkey signal received: forcing light theme");
                    state.ForcedTheme = Theme.Light;
                    ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                    ThemeManager.UpdateTheme(builder.Config, Theme.Light, new(SwitchSource.Manual));
                });
                if (builder.Config.Hotkeys.NoForceHotkey != null) Register(builder.Config.Hotkeys.NoForceHotkey, () =>
                {
                    Logger.Info("hotkey signal received: stop forcing specific theme");
                    state.ForcedTheme = Theme.Unknown;
                    ThemeManager.RequestSwitch(builder, new(SwitchSource.Manual));
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
            foreach (string keyString in splitKeys)
            {
                Keys key = (Keys)converter.ConvertFromInvariantString(keyString);
                keys.Add(key);
            }
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
