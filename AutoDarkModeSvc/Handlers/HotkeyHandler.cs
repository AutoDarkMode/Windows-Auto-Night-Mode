#region copyright
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Core;

namespace AutoDarkModeSvc.Handlers;

internal static class HotkeyHandler
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static Service Service { get; set; }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private static List<HotkeyInternal> Registered { get; } = [];

    public static void RegisterAllHotkeys(AdmConfigBuilder builder)
    {
        _logger.Debug("registering hotkeys");
        try
        {
            GlobalState state = GlobalState.Instance();

            if (builder.Config.Hotkeys.ForceDark != null)
            {
                Register(
                    builder.Config.Hotkeys.ForceDark,
                    () =>
                    {
                        _logger.Info("hotkey signal received: forcing dark theme");
                        state.ForcedTheme = Theme.Dark;
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Dark);
                        ThemeManager.UpdateTheme(new(SwitchSource.Manual, Theme.Dark));
                    }
                );
            }

            if (builder.Config.Hotkeys.ForceLight != null)
            {
                Register(
                    builder.Config.Hotkeys.ForceLight,
                    () =>
                    {
                        _logger.Info("hotkey signal received: forcing light theme");
                        state.ForcedTheme = Theme.Light;
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                        ThemeManager.UpdateTheme(new(SwitchSource.Manual, Theme.Light));
                    }
                );
            }

            if (builder.Config.Hotkeys.NoForce != null)
            {
                Register(
                    builder.Config.Hotkeys.NoForce,
                    () =>
                    {
                        _logger.Info("hotkey signal received: stop forcing specific theme");
                        state.ForcedTheme = Theme.Unknown;
                        ThemeHandler.EnforceNoMonitorUpdates(builder, state, Theme.Light);
                        ThemeManager.RequestSwitch(new(SwitchSource.Manual));
                    }
                );
            }

            if (builder.Config.Hotkeys.ToggleTheme != null)
            {
                Register(
                    builder.Config.Hotkeys.ToggleTheme,
                    () =>
                    {
                        _logger.Info("hotkey signal received: toggle theme");
                        ThemeManager.SwitchThemeAutoPauseAndNotify();
                    }
                );
            }

            if (builder.Config.Hotkeys.ToggleAutoThemeSwitch != null)
            {
                Register(
                    builder.Config.Hotkeys.ToggleAutoThemeSwitch,
                    () =>
                    {
                        _logger.Info("hotkey signal received: toggle automatic theme switch");
                        AdmConfig old = builder.Config;
                        state.SkipConfigFileReload = true;
                        builder.Config.AutoThemeSwitchingEnabled = !builder.Config.AutoThemeSwitchingEnabled;
                        builder.Save();
                        ToastHandler.InvokeAutoSwitchToggleToast();
                    }
                );
            }

            if (builder.Config.Hotkeys.TogglePostpone != null)
            {
                Register(
                    builder.Config.Hotkeys.TogglePostpone,
                    () =>
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
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "could not register hotkeys:");
        }
    }

    public static void UnregisterAllHotkeys()
    {
        _logger.Debug("removing hotkeys");
        Registered.ForEach(hk => Unregister(hk.Id));
        Registered.Clear();
    }

    public static HotkeyInternal GetRegistered(uint modifiers, uint keyCode)
    {
        return Registered.Find(hk => hk.Modifiers == modifiers && hk.KeyCode == keyCode);
    }

    private static void Register(string hotkeyString, Action action)
    {
        if (Service == null)
        {
            _logger.Error("service not instantiated while trying to register hotkey");
            return;
        }

        try
        {
            var parts = hotkeyString.Split(',');
            if (parts.Length != 2 || !uint.TryParse(parts[0], out uint modifiers) || !uint.TryParse(parts[1], out uint keyCode))
            {
                _logger.Error($"Invalid hotkey format: {hotkeyString}");
                return;
            }

            HotkeyInternal mappedHotkey = new()
            {
                StringCode = hotkeyString,
                Modifiers = modifiers,
                KeyCode = keyCode,
                Action = action,
            };

            Registered.Add(mappedHotkey);

            Service.Invoke(new Action(() => RegisterHotKey(Service.Handle, 0, modifiers, keyCode)));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to register hotkey: {hotkeyString}");
        }
    }

    private static void Unregister(int id)
    {
        if (Service == null)
        {
            _logger.Error("service not instantiated while trying to unregister hotkey");
        }
        HotkeyInternal mappedHotkey = Registered.Find(hk => hk.Id == id);
        if (mappedHotkey != null)
        {
            Service.Invoke(new Action(() => UnregisterHotKey(Service.Handle, mappedHotkey.Id)));
        }
    }
}

public class HotkeyInternal
{
    public int Id { get; set; }
    public string StringCode { get; set; }
    public uint Modifiers { get; set; }
    public uint KeyCode { get; set; }
    public Action Action { get; set; }
}
