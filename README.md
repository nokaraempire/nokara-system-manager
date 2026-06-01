# Nokara System Manager

Nokara System Manager is a Windows toolkit for safe maintenance, gaming, streaming and diagnostics. It ships as a premium WPF EXE and also includes a PowerShell Lite script for quick checks without installing anything.

Current app version: `1.1.1`.

Nokara does not promise magic FPS. It helps diagnose common issues, apply safe changes with confirmation, keep logs/backups, and generate readable reports. Real results depend on hardware, Windows, drivers, game settings and background apps.

## Highlights

- WPF desktop app for Windows 10/11.
- Browser-style shell with top tabs and command/address bar.
- Dashboard, Maintenance, Diagnostic, Gaming, Network, Streaming, Processes, Privacy, Backups, Settings and About sections.
- Diagnostic cards for stuttering, RAM, storage, network, games and streaming context.
- Safe maintenance actions with admin detection and status feedback.
- DNS tools, process visibility, backups, action history and final reports.
- PowerShell Lite menu for quick maintenance and diagnostics.
- PowerShell launcher for downloading the latest release EXE from GitHub Releases.

## EXE version

Download the portable EXE from GitHub Releases once a release is published. Binaries are not stored in the repository source tree.

Run:

```powershell
.\NokaraSystemManager.exe
```

Some actions need administrator permissions. The app shows whether it is running as admin and offers a restart-as-admin action when needed.

## PowerShell Lite

PowerShell Lite is a text-mode companion for quick diagnostics and safe maintenance:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1" | iex
```

It includes maintenance, Flush DNS, basic network repair with confirmation, heavy process review, stuttering checks, RAM diagnostics, gaming/streaming checklists, storage scan and report export.

## PowerShell launcher

The launcher fetches the latest release asset from GitHub Releases, optionally verifies a `.sha256` asset, and starts the EXE:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/launch.ps1" | iex
```

Repository owner, repository name, asset name, install path and log path are variables at the top of the script.

## Safety model

Nokara System Manager is designed to avoid risky "optimizer" behavior. It does not:

- Disable Microsoft Defender.
- Disable Windows Firewall.
- Disable Windows Update.
- Use BCDEDIT tweaks.
- Touch HPET.
- Apply MSI Mode.
- Modify GPU drivers or driver settings.
- Touch anti-cheat systems.
- Force Realtime process priority.
- Close processes without confirmation.
- Delete personal files.
- Download external RAM-cleaner binaries.
- Run `chkdsk /f` or `chkdsk /r` automatically.

Storage checks use `chkdsk X: /scan` only, and require explicit confirmation.

## Logs, backups and reports

Runtime data is stored under the current user's AppData:

- Logs: `%AppData%\NokaraSystemManager\Logs`
- Backups: `%AppData%\NokaraSystemManager\Backups`
- Config: `%AppData%\NokaraSystemManager\Config`
- Reports: `%AppData%\NokaraSystemManager\Reports`
- Lite logs: `%AppData%\NokaraSystemManager\LiteLogs`
- Launcher logs: `%AppData%\NokaraSystemManager\LauncherLogs`

Final reports are also written to the Desktop as:

```text
Nokara-Reporte-yyyyMMdd-HHmmss.txt
```

The Backups section can restore supported reversible changes such as DNS, registry-based settings and power-plan changes when a backup exists.

## Build

Requirements:

- Windows 10/11.
- .NET SDK `10.0.300` or compatible SDK with `net10.0-windows` support.
- PowerShell 5.1+ for the scripts.

Build:

```powershell
dotnet build
```

## Publish

Create a self-contained single-file Windows x64 publish:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Publish output goes to:

```text
bin/Release/net10.0-windows/win-x64/publish/
```

Do not commit publish outputs or EXE binaries to the repository. Upload release binaries to GitHub Releases instead.

## Screenshots

Screenshots can be stored in:

```text
docs/screenshots/
```

Recommended screenshots:

- Browser shell / Dashboard.
- Diagnostic.
- Processes.
- Maintenance.
- Final report.

## GitHub releases

Suggested release assets:

- `NokaraSystemManager.exe`
- `NokaraSystemManager.exe.sha256`
- `RELEASE_NOTES_v1.1.1.md`

SmartScreen may warn users because the EXE is unsigned or newly published. Users should only download releases from the official Nokara Empire GitHub account.

## Nokara Empire

- GitHub: https://github.com/nokaraempire
- Instagram: https://instagram.com/nokaraempire
- TikTok: https://tiktok.com/@nokaraempire
- YouTube: https://youtube.com/@nokaraempire
- Issues: https://github.com/nokaraempire/nokara-system-manager/issues

## Credits

Created by Nokara Empire. Developed by kkshi / Fabian Almada.

## Disclaimer

Use this tool at your own discretion. Always make backups before changing system settings. Nokara System Manager focuses on safe diagnostics and reversible maintenance, but no tool can guarantee performance improvements on every PC.
