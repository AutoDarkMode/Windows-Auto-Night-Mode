# x64 MSI package

Run `pwsh -File packaging/Build-Msi.ps1` from the repository root. This needs
.NET 10 SDK, PowerShell 7 and WiX CLI 4.0.6. Install WiX into a local tool
directory with:

```powershell
dotnet tool install wix --tool-path ../.local-tools/wix --version 4.0.6
```

The script restores and publishes the settings app and background service as
self-contained `win-x64` applications, adds the shell helper as a self-contained
single file, then builds
`bin/Msi/AutoDarkMode-Ambient-x64.msi`. For an already verified staging tree,
`pwsh -File packaging/Build-Msi.ps1 -SkipPublish` skips publishing.

The MSI is a current-user installer. It places the application at
`%LOCALAPPDATA%/Programs/AutoDarkMode-Ambient/adm-app`, with `core` and `ui`
subdirectories required by the app, and adds a Start menu shortcut. Launching
the settings app starts the companion background service. The included UF2 is
for RP2040 + BH1750 hardware. Uninstall removes MSI-managed files and the
shortcut; user settings under AppData are not deleted.

The upstream Rust auto-updater is deliberately not bundled. It targets the
upstream release channel and could replace the fork's ambient-light features.
Update this MSI with a later fork build instead.

This fork has a separate MSI product identity and directory from upstream Auto
Dark Mode. Quit any older running service before launching this installation so
the single-instance background service does not redirect to that older build.
The MSI is unsigned. It has not been installed on a clean Windows machine as
part of this build; verify that workflow before publishing it as a release.

The build has been checked with `wix msi validate -sice ICE03 -sice ICE91`.
`ICE03` flags language identifiers embedded in some upstream multilingual
runtime files; `ICE91` warns about a per-user destination even though the
package explicitly uses `Scope="perUser"`. These checks are excluded only for
validation; the payload keeps all translations. An administrative image extract
(`msiexec /a`) was also checked for the service, settings executable, shell
helper and UF2. The MSI has not been executed as a normal install on this
machine; a clean-machine install/uninstall test remains to be done.
