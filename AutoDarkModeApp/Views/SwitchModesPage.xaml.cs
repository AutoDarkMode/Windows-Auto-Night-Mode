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
        // 使用 InputKeyboardSource 获取按键状态
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    // 将 VirtualKey 转换为友好字符串
    private static string GetKeyString(VirtualKey key)
    {
        // 处理字母键
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return key.ToString().ToUpper();
        }

        // 处理数字键
        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return ((int)(key - VirtualKey.Number0)).ToString();
        }

        // 处理功能键
        if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
        {
            return key.ToString();
        }

        // 其他特殊键映射
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
        // 获取按下的键
        var key = e.Key;

        // 检测修饰键状态
        var isCtrl = IsKeyDown(VirtualKey.Control);
        var isShift = IsKeyDown(VirtualKey.Shift);
        var isAlt = IsKeyDown(VirtualKey.Menu);
        var isWin = IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows);

        // 构建快捷键字符串
        var hotkey = "";
        hotkey += isCtrl ? "Ctrl + " : "";
        hotkey += isShift ? "Shift + " : "";
        hotkey += isAlt ? "Alt + " : "";
        hotkey += isWin ? "Win + " : "";
        hotkey += GetKeyString(key);

        // 更新 TextBlock 显示内容
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

        // 标记事件已处理
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
