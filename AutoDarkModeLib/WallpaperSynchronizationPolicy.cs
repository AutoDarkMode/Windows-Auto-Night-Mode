namespace AutoDarkModeLib;

public static class WallpaperSynchronizationPolicy
{
    // The internal Shell ABI has been read-tested on this build. Other builds
    // keep their existing wallpaper behavior until their ABI is verified.
    public static bool IsSupportedBuild(int build) => build == 26220;
    public static bool ShouldSynchronize(bool enabled, bool globalPicture, int build)
        => enabled && globalPicture && IsSupportedBuild(build);
}
