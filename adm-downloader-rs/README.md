adm-downloader-rs
=================

Small helper that downloads the correct AutoDarkMode Inno Setup installer (x86/ARM64), verifies its SHA256, runs it and forwards the installer's exit code.

Usage
-----
- Run the binary directly and pass any installer CLI args; they will be forwarded to the installer.
  Example:
  `adm-downloader-rs /verysilent`

Exit codes
----------
| Exit Code | Description |
|-----------|-------------|
| 0 | Success (installer ran and returned 0, or nothing to do) |
| 13370 | Download failed (couldn't fetch the installer) |
| 13371 | SHA256 verification failed (download corrupted or mismatch) |
| 13372 | Failed to spawn the installer process |
| 13373 | Failed to remove the temporary downloaded file during cleanup |
| Other | Any other non-zero code returned by the installer will be forwarded by this tool |

Notes
-----
- The tool prefers the installer's exit code when available.
- If the installer does not provide an exit code, the tool uses its own mapped codes as above.
- Pass-through arguments: any args you pass to this wrapper are appended to the installer command line.

License
-------
See project license file.
