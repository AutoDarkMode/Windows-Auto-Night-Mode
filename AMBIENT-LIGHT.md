# Ambient light page

This fork adds ambient-light controls, hardware brightness curves and an optional
[all-virtual-desktop wallpaper switch](docs/virtual-desktop-wallpaper.md).

The sidebar's **Ambient light sensor** page uses Auto Dark Mode's existing
SettingsCard, section-heading, page-width, spacing, and theme resources.

* **Current** is a page-wide Windows HID lux reading. It remains available with
  both automation switches off and feeds the theme indicator and curve preview.
  Missing readings are displayed as unavailable, rather than being treated as zero.
* **Automatic theme switching** moves the existing thresholds, logarithmic range
  selector, auto-configuration, and illuminance reference out of the time page.
  Theme switching still uses Auto Dark Mode's ambient governor, debounce and
  switching components. Enabling time rules selects the time governor; enabling
  ambient theme switching selects the ambient governor. These two theme sources
  are mutually exclusive. Brightness has its own independent switch.
* **Brightness** provides draggable and keyboard-editable control points, numeric
  editing, point insertion/removal, named presets, and a current-lux marker. The
  axis stays at 0–10,000 lux with subdivisions inside each logarithmic decade.
  Control points above 10,000 lux are rejected; higher live readings stay at the
  chart edge. Valid edits automatically save within 250 ms; finishing a drag saves
  immediately. No Apply button is required. The adjustment-speed card defaults to
  4 percentage points per second and accepts 0.1–20. New installations default off.
* **Pico firmware** bundles the RP2040 UF2 from
  `artifacts/AdaptiveBrightness-win-x64-switch-layout`. The button validates the
  image and RPI-RP2 bootloader identity, asks for confirmation in the app, then
  rechecks the target before copying. Multiple matching boot drives are rejected.

## Runtime

`AutoDarkModeSvc/AmbientBrightnessHost.cs` owns the CDC/DDC controller in the tray
service. Leaving the page or closing the settings window does not stop it.
The UI does not open CDC or write DDC brightness. Configuration is stored separately
in `%LOCALAPPDATA%\AutoDarkMode\ambient-brightness.json`; a nearby `.status` file
provides the background controller's live status. Changes are read every 250 ms.
Curve and speed changes update the running controller without reconnecting CDC
or restarting the brightness ramp. Invalid numeric edits retain the last valid settings.

The controller uses the configured rate limit, a 500 ms minimum write interval,
actual VCP maximum detection, reconnect logic,
and controller ownership protection. Existing Adaptive Brightness controllers are
reported as conflicts rather than taking their serial port. A serial/disconnect
failure retains the display's last brightness.

The settings app and service target .NET 10. `AdaptiveBrightness.Core` is included
inside this checkout so deployment does not depend on files outside it.

## Build and checks

```powershell
dotnet build AutoDarkModeApp/AutoDarkModeApp.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
dotnet build AutoDarkModeSvc/AutoDarkModeSvc.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
dotnet run --project tests/AmbientLight.Checks
```

The logic checks validate saved curves/switch state, log-lux interpolation, invalid
control points, the bundled firmware, and boot-drive identity. They do not write
firmware or display brightness. The new service must be running to use brightness
automation; an already installed older service does not implement this feature.

## Verified on 2026-09-27

* Settings app and tray service built successfully; 23 logic checks passed.
* The service output now omits the runtime-ID subdirectory, matching the required
  `adm-app/core/AutoDarkModeSvc.exe` layout. The earlier preview had connected to
  the installed older service, which lacked the brightness module.
* The new service was started from this checkout. At approximately 86 lux, actual
  VCP readback rose from 8% through 10, 13, 15, 17, 19, 21, and 23% toward the
  saved 59.2% target. After the user saved another curve, readback decreased through
  56, 54, 52, and 50% to 48%, matching the new curve. This verifies real DDC writes
  and the time ramp during a curve change, without synthetic sensor input.
* The native window displayed the subdivided axis ending at 10,000, the saved
  curve, and live service status. No firmware flashing was performed.

## Known remaining theme issues

The inherited ambient theme governor still lacks sensor rediscovery after starting
without a sensor, and its wall-clock debounce check can leave a pending request
stalled after a clock rollback. Both were reproduced in an isolated source-linked
harness; these changes do not claim to fix them.
