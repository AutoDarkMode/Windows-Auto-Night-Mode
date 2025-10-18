using CommunityToolkit.Mvvm.ComponentModel;
using Windows.System;

namespace AutoDarkModeApp.Models;

public partial class HotkeysDataObject : ObservableObject
{
    [ObservableProperty]
    public partial string? DisplayName { get; set; }

    [ObservableProperty]
    public partial string? Keys { get; set; }

    [ObservableProperty]
    public partial string? Tag { get; set; }

    public string? DisplayKeys => ConvertToDisplayFormat(Keys);

    private static string? ConvertToDisplayFormat(string? hotkeyConfig)
    {
        if (string.IsNullOrEmpty(hotkeyConfig))
        {
            return null;
        }

        var parts = hotkeyConfig.Split(',');
        if (parts.Length != 2 || !uint.TryParse(parts[0], out uint modifiers) || !uint.TryParse(parts[1], out uint keyCode))
        {
            return hotkeyConfig;
        }

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

        return string.Join(" + ", displayParts);
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

    partial void OnKeysChanged(string? value)
    {
        OnPropertyChanged(nameof(DisplayKeys));
    }
}
