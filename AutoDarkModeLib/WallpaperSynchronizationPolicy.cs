namespace AutoDarkModeLib;

public static class WallpaperSynchronizationPolicy
{
    public static bool ShouldSynchronize(bool enabled, bool globalPicture)
        => enabled && globalPicture;
}
