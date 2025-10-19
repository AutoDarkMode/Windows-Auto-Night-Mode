using AutoDarkModeLib;
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

    [GeneratedDependencyProperty]
    public partial string? CapturedHotkeys { get; set; }

    public void LoadFromKeyValue(string? hotkeyValue)
    {
        if (string.IsNullOrWhiteSpace(hotkeyValue))
        {
            HotkeyCombination = null;
            CapturedHotkeys = null;
            return;
        }

        CapturedHotkeys = HotkeyStringConverter.ToDisplayFormat(hotkeyValue);
        var displayParts = hotkeyValue.Split('+', StringSplitOptions.TrimEntries);
        HotkeyCombination = displayParts.Select(part => new SingleHotkeyDataObject { Key = part }).ToList();
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
            displayParts.Add(HotkeyStringConverter.GetKeyDisplayName(key));
        }

        CapturedHotkeys = string.Join(" + ", displayParts);

        HotkeyCombination = displayParts.Select(p => new SingleHotkeyDataObject { Key = p }).ToList();

        e.Handled = true;
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var keyboardState = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return keyboardState.HasFlag(CoreVirtualKeyStates.Down);
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
