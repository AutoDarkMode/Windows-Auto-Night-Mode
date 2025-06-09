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
        var clearTextBox = IsKeyDown(VirtualKey.Escape) || IsKeyDown(VirtualKey.Back) || IsKeyDown(VirtualKey.Delete); // NOTE: this doesn't work anymore  

        // Build an array of modifier strings, then join with " + " as delimiter  
        var modifiers = new List<string>();
        if (isCtrl) modifiers.Add("Ctrl");
        if (isShift) modifiers.Add("Shift");
        if (isAlt) modifiers.Add("Alt");
        if (isWin) modifiers.Add("LWin");

        var keyString = GetKeyString(key);
        if (!string.IsNullOrEmpty(keyString))
            modifiers.Add(keyString);

        var hotkeyString = string.Join(" + ", modifiers); // Renamed variable to 'hotkeyString' to avoid conflict  

        if (string.IsNullOrEmpty(GetKeyString(key)) || !isCtrl && !isShift && !isAlt && !isWin && !clearTextBox)
        {
            e.Handled = true;
            return;
        }

        if (clearTextBox)
        {
            hotkeyString = null;
            Keys = [];
        }
        else
        {
            Keys = hotkeyString.Split(" + ").Select(key => new SingleHotkeyDataObject { Key = key }).ToList();
        }

        CapturedHotkeys = hotkeyString;

        e.Handled = true;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    private static string GetKeyString(VirtualKey key)
    {
        if (key is >= VirtualKey.A and <= VirtualKey.Z)
            return key.ToString().ToUpper();

        if (key is >= VirtualKey.Number0 and <= VirtualKey.Number9)
            return (key - VirtualKey.Number0).ToString();

        if (key is >= VirtualKey.F1 and <= VirtualKey.F24)
            return key.ToString();

        if (key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows)
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
            VirtualKey.CapitalLock => "CapsLock",
            _ => key.ToString(),
        };
    }
}

public class SingleHotkeyDataObject
{
    public string? Key { get; set; }
}
