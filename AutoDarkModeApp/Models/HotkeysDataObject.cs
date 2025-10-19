using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.Models;

public partial class HotkeysDataObject : ObservableObject
{
    [ObservableProperty]
    public partial string? DisplayName { get; set; }

    [ObservableProperty]
    public partial string? Keys { get; set; }

    [ObservableProperty]
    public partial string? Tag { get; set; }

    public string? DisplayKeys => HotkeyStringConverter.ToDisplayFormat(Keys);

    partial void OnKeysChanged(string? value)
    {
        OnPropertyChanged(nameof(DisplayKeys));
    }
}
