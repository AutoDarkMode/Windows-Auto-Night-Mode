using AutoDarkModeApp.Utils;
using AutoDarkModeLib;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ShortcutDialogContentControl : UserControl
{
    [GeneratedDependencyProperty]
    public partial List<SingleHotkeyDataObject>? HotkeyCombination { get; set; }

    [GeneratedDependencyProperty]
    public partial string? CapturedHotkeys { get; set; }

    private KeyboardHook? _keyboardHook;
    private bool _isCapturing;

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
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartCapturing();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StopCapturing();
    }

    private void StartCapturing()
    {
        if (_keyboardHook == null)
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyEvent += OnKeyboardEvent;
        }
        _isCapturing = true;
        _keyboardHook.Install();
    }

    private void StopCapturing()
    {
        _isCapturing = false;
        _keyboardHook?.Uninstall();
    }

    private void OnKeyboardEvent(object? sender, KeyboardHookEventArgs e)
    {
        if (!_isCapturing || _keyboardHook is null)
        {
            return;
        }

        if (e.VirtualKeyCode == (int)VirtualKey.Tab || e.IsKeyUp)
        {
            return;
        }

        var isCtrl = _keyboardHook.IsKeyDown(VirtualKey.Control) || _keyboardHook.IsKeyDown(VirtualKey.LeftControl) || _keyboardHook.IsKeyDown(VirtualKey.RightControl);
        var isShift = _keyboardHook.IsKeyDown(VirtualKey.Shift) || _keyboardHook.IsKeyDown(VirtualKey.LeftShift) || _keyboardHook.IsKeyDown(VirtualKey.RightShift);
        var isAlt = _keyboardHook.IsKeyDown(VirtualKey.Menu) || _keyboardHook.IsKeyDown(VirtualKey.LeftMenu) || _keyboardHook.IsKeyDown(VirtualKey.RightMenu);
        var isWin = _keyboardHook.IsKeyDown(VirtualKey.LeftWindows) || _keyboardHook.IsKeyDown(VirtualKey.RightWindows);

        if (!(isCtrl || isShift || isAlt || isWin))
        {
            e.Handled = true;
            return;
        }

        List<string> displayParts = [];
        if (isCtrl) displayParts.Add("Ctrl");
        if (isShift) displayParts.Add("Shift");
        if (isAlt) displayParts.Add("Alt");
        if (isWin) displayParts.Add("Win");

        var key = (VirtualKey)e.VirtualKeyCode;
        if (!IsModifierKey(key))
        {
            displayParts.Add(HotkeyStringConverter.GetKeyDisplayName(key));
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            CapturedHotkeys = string.Join(" + ", displayParts);
            HotkeyCombination = displayParts.Select(p => new SingleHotkeyDataObject { Key = p }).ToList();
        });

        e.Handled = true;
    }

    private static bool IsModifierKey(VirtualKey key)
    {
        return key == VirtualKey.Control
            || key == VirtualKey.LeftControl
            || key == VirtualKey.RightControl
            || key == VirtualKey.Shift
            || key == VirtualKey.LeftShift
            || key == VirtualKey.RightShift
            || key == VirtualKey.Menu
            || key == VirtualKey.LeftMenu
            || key == VirtualKey.RightMenu
            || key == VirtualKey.LeftWindows
            || key == VirtualKey.RightWindows;
    }
}

public class SingleHotkeyDataObject
{
    public string? Key { get; set; }
}
