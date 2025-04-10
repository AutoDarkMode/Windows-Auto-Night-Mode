using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace AutoDarkModeApp.Views;

public sealed partial class SwitchModesPage : Page
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public SwitchModesViewModel ViewModel
    {
        get;
    }

    public SwitchModesPage()
    {
        ViewModel = App.GetService<SwitchModesViewModel>();
        InitializeComponent();
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    private static string GetKeyString(VirtualKey key)
    {
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return key.ToString().ToUpper();
        }

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return ((int)(key - VirtualKey.Number0)).ToString();
        }

        if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
        {
            return key.ToString();
        }

        return key switch
        {
            VirtualKey.Add => "+",
            VirtualKey.Subtract => "-",
            VirtualKey.Multiply => "*",
            VirtualKey.Divide => "/",
            VirtualKey.Decimal => ".",
            VirtualKey.Enter => "Enter",
            VirtualKey.Escape => "Esc",
            VirtualKey.Space => "Space",
            VirtualKey.Tab => "Tab",
            VirtualKey.Back => "Backspace",
            VirtualKey.Delete => "Del",
            VirtualKey.PageUp => "PgUp",
            VirtualKey.PageDown => "PgDn",
            VirtualKey.Left => "←",
            VirtualKey.Right => "→",
            VirtualKey.Up => "↑",
            VirtualKey.Down => "↓",
            VirtualKey.CapitalLock => "CapsLock",
            _ => key.ToString()
        };
    }

    private void HotkeyTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key;

        var isCtrl = IsKeyDown(VirtualKey.Control);
        var isShift = IsKeyDown(VirtualKey.Shift);
        var isAlt = IsKeyDown(VirtualKey.Menu);
        var isWin = IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows);

        var hotkey = "";
        hotkey += isCtrl ? "Ctrl + " : "";
        hotkey += isShift ? "Shift + " : "";
        hotkey += isAlt ? "Alt + " : "";
        hotkey += isWin ? "Win + " : "";
        hotkey += GetKeyString(key);

        if (!string.IsNullOrEmpty(hotkey))
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = hotkey;
            }
        }

        if ((TextBox)sender == HotkeyForceLightTextBox)
        {
            _builder.Config.Hotkeys.ForceLight = hotkey;
        }
        else if ((TextBox)sender == HotkeyForceDarkTextBox)
        {
            _builder.Config.Hotkeys.ForceDark = hotkey;
        }
        else if ((TextBox)sender == HotkeyHeaderNoForceTextBox)
        {
            _builder.Config.Hotkeys.NoForce = hotkey;
        }
        else if ((TextBox)sender == HotkeyHeaderToggleThemeTextBox)
        {
            _builder.Config.Hotkeys.ToggleTheme = hotkey;
        }
        else if ((TextBox)sender == HotkeyHeaderToggleAutomaticThemeTextBox)
        {
            _builder.Config.Hotkeys.ToggleAutoThemeSwitch = hotkey;
        }
        else if ((TextBox)sender == HotkeyHeaderTogglePostponeTextBox)
        {
            _builder.Config.Hotkeys.TogglePostpone = hotkey;
        }
        _builder.Save();

        e.Handled = true;
    }

    private void HotkeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (textBox.Text == "")
            {
                if ((TextBox)sender == HotkeyForceLightTextBox)
                {
                    _builder.Config.Hotkeys.ForceLight = "";
                }
                else if ((TextBox)sender == HotkeyForceDarkTextBox)
                {
                    _builder.Config.Hotkeys.ForceDark = "";
                }
                else if ((TextBox)sender == HotkeyHeaderNoForceTextBox)
                {
                    _builder.Config.Hotkeys.NoForce = "";
                }
                else if ((TextBox)sender == HotkeyHeaderToggleThemeTextBox)
                {
                    _builder.Config.Hotkeys.ToggleTheme = "";
                }
                else if ((TextBox)sender == HotkeyHeaderToggleAutomaticThemeTextBox)
                {
                    _builder.Config.Hotkeys.ToggleAutoThemeSwitch = "";
                }
                else if ((TextBox)sender == HotkeyHeaderTogglePostponeTextBox)
                {
                    _builder.Config.Hotkeys.TogglePostpone = "";
                }
            }
        }
        _builder.Save();
    }
}
