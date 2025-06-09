using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ShortcutDialogContentControl : UserControl
{
    public List<SingleHotkeyDataObject> Keys
    {
        get => (List<SingleHotkeyDataObject>)GetValue(KeysProperty);
        set => SetValue(KeysProperty, value);
    }

    public static readonly DependencyProperty KeysProperty = DependencyProperty.Register(
        "Keys",
        typeof(List<SingleHotkeyDataObject>),
        typeof(ShortcutDialogContentControl),
        new PropertyMetadata(default(string))
    );

    public string? CapturedHotkeys
    {
        get => (string?)GetValue(CapturedHotkeysProperty);
        set => SetValue(CapturedHotkeysProperty, value);
    }

    public static readonly DependencyProperty CapturedHotkeysProperty = DependencyProperty.Register(
        "CapturedHotkeys",
        typeof(string),
        typeof(ShortcutDialogContentControl),
        new PropertyMetadata(default(string))
    );

    public ShortcutDialogContentControl()
    {
        InitializeComponent();
    }

    private void StackPanel_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var key = e.Key;

        var isCtrl = IsKeyDown(VirtualKey.Control);
        var isShift = IsKeyDown(VirtualKey.Shift);
        var isAlt = IsKeyDown(VirtualKey.Menu);
        var isWin = IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows);
        var clearTextBox = IsKeyDown(VirtualKey.Escape) || IsKeyDown(VirtualKey.Back) || IsKeyDown(VirtualKey.Delete);

        var hotkey = "";
        hotkey += isCtrl ? "Ctrl + " : "";
        hotkey += isShift ? "Shift + " : "";
        hotkey += isAlt ? "Alt + " : "";
        hotkey += isWin ? "LWin + " : "";
        hotkey += GetKeyString(key);

        if (string.IsNullOrEmpty(GetKeyString(key)) || (!isCtrl && !isShift && !isAlt && !isWin) && !clearTextBox)
        {
            e.Handled = true;
            return;
        }

        if (clearTextBox)
        {
            hotkey = null;
        }
        else
        {
            Keys = hotkey.Split('+').Select(key => new SingleHotkeyDataObject { Key = key }).ToList();
        }

        CapturedHotkeys = hotkey;

        e.Handled = true;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    private static string GetKeyString(VirtualKey key)
    {
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
            return key.ToString().ToUpper();

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
            return ((int)(key - VirtualKey.Number0)).ToString();

        if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
            return key.ToString();

        if (key == VirtualKey.Control || key == VirtualKey.Shift || key == VirtualKey.Menu || key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            return "";

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
            VirtualKey.Left => "¡û",
            VirtualKey.Right => "¡ú",
            VirtualKey.Up => "¡ü",
            VirtualKey.Down => "¡ý",
            VirtualKey.CapitalLock => "CapsLock",
            _ => key.ToString(),
        };
    }
}

public class SingleHotkeyDataObject
{
    public string? Key { get; set; }
}
