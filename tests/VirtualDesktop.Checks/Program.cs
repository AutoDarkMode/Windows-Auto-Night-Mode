using AutoDarkModeSvc.Handlers;

if (args.Contains("--read") || args.Contains("--apply"))
{
    if (args.Contains("--apply")) Console.WriteLine($"Synchronized desktops: {VirtualDesktopWallpaper.Synchronize(args[^1])}");
    foreach (var desktop in VirtualDesktopWallpaper.ReadWallpapers()) Console.WriteLine($"{desktop.Id}: {desktop.Path}");
    return;
}
var desktops = new[] {
    new VirtualDesktopWallpaper.DesktopWallpaper(Guid.NewGuid(), @"C:\light.jpg"),
    new VirtualDesktopWallpaper.DesktopWallpaper(Guid.NewGuid(), @"C:\dark.png"),
    new VirtualDesktopWallpaper.DesktopWallpaper(Guid.NewGuid(), @"C:\light.jpg")
};
if (VirtualDesktopWallpaper.AllMatch(desktops, @"C:\dark.png")) throw new Exception("One correct desktop must not mark all desktops synchronized");
if (!VirtualDesktopWallpaper.AllMatch(desktops.Select(d => d with { Path = @"C:\DARK.PNG" }).ToArray(), @"C:\dark.png")) throw new Exception("Paths must compare case-insensitively");
if (VirtualDesktopWallpaper.AllMatch([], @"C:\dark.png")) throw new Exception("An empty enumeration is not success");
if (VirtualDesktopWallpaper.IsSupportedBuild(22621) || VirtualDesktopWallpaper.IsSupportedBuild(99999)) throw new Exception("Unknown ABI must be rejected");
if (AutoDarkModeLib.WallpaperSynchronizationPolicy.ShouldSynchronize(false, true, 26220)) throw new Exception("Disabled option must never synchronize other desktops");
if (AutoDarkModeLib.WallpaperSynchronizationPolicy.ShouldSynchronize(true, false, 26220)) throw new Exception("Independent monitor/Spotlight modes must remain unchanged");
if (!AutoDarkModeLib.WallpaperSynchronizationPolicy.ShouldSynchronize(true, true, 26220)) throw new Exception("Opt-in global picture mode must synchronize");
Console.WriteLine("7 checks passed; use --read for a read-only native check.");
