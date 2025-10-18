using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ShortcutDialogContentControl : UserControl
{
    [GeneratedDependencyProperty]
    public partial List<SingleHotkeyDataObject>? HotkeyCombination { get; set; }

    // For developers:
    // With regard to hotkeys, the storage format is: "Decorative key code, primary key code"
    // Modifier key marks: Control=2, Shift=4, Alt=1, Win=8 (can be bitwise or combined)
    // In Builder, they looks like: "ForceLight": "6,112"
    [GeneratedDependencyProperty]
    public partial string? CapturedHotkeys { get; set; }

    public void LoadFromConfig(string? hotkeyString)
    {
        if (string.IsNullOrEmpty(hotkeyString))
        {
            HotkeyCombination = null;
            CapturedHotkeys = null;
            return;
        }

        var parts = hotkeyString.Split(',');
        if (parts.Length != 2 || !uint.TryParse(parts[0], out uint modifiers) || !uint.TryParse(parts[1], out uint keyCode))
        {
            HotkeyCombination = null;
            CapturedHotkeys = null;
            return;
        }

        CapturedHotkeys = hotkeyString;

        List<string> displayParts = [];
        if ((modifiers & 2) != 0)
        {
            displayParts.Add("Ctrl");
        }
        if ((modifiers & 4) != 0)
        {
            displayParts.Add("Shift");
        }
        if ((modifiers & 1) != 0)
        {
            displayParts.Add("Alt");
        }
        if ((modifiers & 8) != 0)
        {
            displayParts.Add("Win");
        }
        displayParts.Add(GetKeyDisplayName((VirtualKey)keyCode));

        HotkeyCombination = displayParts.Select(p => new SingleHotkeyDataObject { Key = p }).ToList();
    }

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

        var isCtrl = IsKeyDown(VirtualKey.Control) || IsKeyDown(VirtualKey.RightControl);
        var isShift = IsKeyDown(VirtualKey.Shift) || IsKeyDown(VirtualKey.RightShift);
        var isAlt = IsKeyDown(VirtualKey.Menu);
        var isWin = IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows);

        if (!(isCtrl || isShift || isAlt || isWin))
        {
            e.Handled = true;
            return;
        }

        // MOD_ALT = 0x0001
        // MOD_CONTROL = 0x0002
        // MOD_SHIFT = 0x0004
        // MOD_WIN = 0x0008
        uint modifiers = 0;
        if (isAlt)
        {
            modifiers |= 1;
        }
        if (isCtrl)
        {
            modifiers |= 2;
        }
        if (isShift)
        {
            modifiers |= 4;
        }
        if (isWin)
        {
            modifiers |= 8;
        }

        CapturedHotkeys = $"{modifiers},{(uint)key}";

        List<string> displayParts = [];
        if (isCtrl)
        {
            displayParts.Add("Ctrl");
        }
        if (isShift)
        {
            displayParts.Add("Shift");
        }
        if (isAlt)
        {
            displayParts.Add("Alt");
        }
        if (isWin)
        {
            displayParts.Add("Win");
        }

        if (!IsModifierKey(key))
        {
            displayParts.Add(GetKeyDisplayName(key));
        }

        HotkeyCombination = displayParts.Select(p => new SingleHotkeyDataObject { Key = p }).ToList();

        e.Handled = true;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
    }

    private static string GetKeyDisplayName(VirtualKey key)
    {
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
            (VirtualKey)188 => ",",
            (VirtualKey)190 => ".",
            (VirtualKey)191 => "/",
            (VirtualKey)187 => "=",
            (VirtualKey)189 => "-",
            (VirtualKey)219 => "[",
            (VirtualKey)221 => "]",
            (VirtualKey)220 => "\\",
            (VirtualKey)186 => ";",
            (VirtualKey)222 => "'",
            (VirtualKey)192 => "`",
            _ => key.ToString(),
        };
    }

    private static bool IsModifierKey(VirtualKey key)
    {
        return key == VirtualKey.Control
            || key == VirtualKey.RightControl
            || key == VirtualKey.Shift
            || key == VirtualKey.RightShift
            || key == VirtualKey.Menu
            || key == VirtualKey.LeftWindows
            || key == VirtualKey.RightWindows;
    }
}

public class SingleHotkeyDataObject
{
    public string? Key { get; set; }
}
