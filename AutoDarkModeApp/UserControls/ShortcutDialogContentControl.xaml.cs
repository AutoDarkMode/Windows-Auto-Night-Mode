using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ShortcutDialogContentControl : UserControl
{
    public List<SingleHotkeyDataObject> HotkeyCombination
    {
        get => (List<SingleHotkeyDataObject>)GetValue(HotkeyCombinationProperty);
        set => SetValue(HotkeyCombinationProperty, value);
    }

    public static readonly DependencyProperty HotkeyCombinationProperty = DependencyProperty.Register(
        "HotkeyCombination",
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
        if (IsKeyDown(VirtualKey.Tab))
        {
            e.Handled = false;
            return;
        }

        VirtualKey key = e.Key;
        string keyString = GetKeyString(key);

        var isCtrl = IsKeyDown(VirtualKey.Control);
        var isShift = IsKeyDown(VirtualKey.Shift);
        var isAlt = IsKeyDown(VirtualKey.Menu);
        var isWin = IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows);

        if (string.IsNullOrEmpty(keyString) || !(isCtrl || isShift || isAlt || isWin))
        {
            e.Handled = true;
            return;
        }

        // Updated code to handle null values explicitly and avoid CS8604
        List<string> modifiers = new List<string>
        {
            isCtrl ? "Ctrl" : string.Empty,
            isShift ? "Shift" : string.Empty,
            isAlt ? "Alt" : string.Empty,
            isWin ? "Win" : string.Empty,
            keyString
        }.Where(modifier => !string.IsNullOrEmpty(modifier))
        .ToList();

        HotkeyCombination = modifiers.Select(mod => new SingleHotkeyDataObject { Key = mod }).ToList();
        CapturedHotkeys = string.Join(" + ", modifiers);

        e.Handled = true;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    private static string GetKeyString(VirtualKey key)
    {
        if (key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows)
            return "";

        return key switch
        {
            VirtualKey.Enter => "Enter",
            VirtualKey.Escape => "Esc",
            VirtualKey.Space => "Space",

            VirtualKey.Back => "Backspace",
            VirtualKey.Delete => "Del",
            VirtualKey.PageUp => "PgUp",
            VirtualKey.PageDown => "PgDn",
            VirtualKey.CapitalLock => "CapsLock",

            (VirtualKey)188 => "OemComma", // ,
            (VirtualKey)190 => "OemPeriod", // .
            (VirtualKey)191 => "OemQuestion", // /
            (VirtualKey)187 => "OemPlus", // =
            (VirtualKey)189 => "OemMinus", // -
            (VirtualKey)219 => "OemOpenBrackets", // [
            (VirtualKey)221 => "OemCloseBrackets", // ]
            (VirtualKey)220 => "OemPipe", // \
            (VirtualKey)186 => "OemSemicolon", // ;
            (VirtualKey)222 => "OemQuotes", // '
            (VirtualKey)192 => "OemTilde", // `
            _ => key.ToString(),
        };
    }
}

public class SingleHotkeyDataObject
{
    public string? Key { get; set; }
}
