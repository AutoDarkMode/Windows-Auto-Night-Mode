# Optional virtual desktop wallpaper synchronization

In **Personalization > Background**, **Switch wallpaper on all virtual desktops**
is an independent option, disabled by default. It applies to Picture mode
(`WallpaperType.Global`). Turning it on replaces each virtual desktop's independent
wallpaper with the configured image for the active light/dark theme. Turning it off
stops synchronization; it does not restore earlier per-desktop choices. Individual
monitor pictures, solid colors and Spotlight are not synchronized by this option.

## Cause and implementation

The existing global wallpaper path uses `SystemParametersInfo` and a single cached
`currentGlobalTheme`. On the test machine, Explorer retained different per-desktop
images despite the active Auto Dark Mode theme. Switching virtual desktops could
therefore reveal an older image without another theme-switch event in the service log.

`VirtualDesktopWallpaper` enumerates Explorer's actual desktop objects and uses
`UpdateWallpaperPathForAllDesktops` to synchronize them without navigating between
desktops. Synchronization runs in the wallpaper component's callback, after the
managed theme file has been applied. It then reads every desktop back and logs a
success only when all paths match. Startup also checks inactive desktops when the
option is enabled, so a matching current desktop does not hide stale backgrounds.
Failed global wallpaper writes no longer advance the component's cached theme.

The COM interface is internal and version-specific. It is currently gated to
Windows build 26220, the build used for local inspection; other builds retain the
existing behavior and show the option as unavailable. Interface exceptions are
logged and preserve current-desktop behavior, without repeated theme applications.
The adapter creates and releases COM objects on an STA thread and marshals HSTRING
explicitly for modern .NET.

Interface declarations derive from the MIT-licensed
[MScholtes VirtualDesktop Windows 11 24H2 implementation](https://github.com/MScholtes/VirtualDesktop/blob/master/VirtualDesktop11-24H2.cs).
The license is included in `ThirdParty/VirtualDesktop-LICENSE.txt`.

## Validation and remaining limits

```powershell
dotnet run --project tests/VirtualDesktop.Checks
dotnet run --project tests/VirtualDesktop.Checks -- --read
```

Seven logic checks pass, covering mismatched desktops, empty enumeration,
case-insensitive paths, opt-in behavior, mode isolation and unsupported builds.
The actual native read-only call succeeded on Windows 26220.9568 and returned all
three virtual desktops with their distinct wallpapers. The settings app and service
compile successfully. The option remains off by default.

**Native all-desktop writes and elimination of the visual flash are not yet
verified.** The user requested an explicit option instead of an automatic overwrite.
After choosing to enable it, validate light-to-dark and dark-to-light switches,
read back all wallpaper paths, and switch desktops to check for a visible flash.
The diagnostic `--apply <image-path>` argument deliberately modifies every desktop;
it is an explicit manual test, not part of the default checks.
