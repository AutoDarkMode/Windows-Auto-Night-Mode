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
}
