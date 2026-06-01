# Nokara System Manager

Nokara System Manager is a Windows toolkit for safe maintenance, gaming, streaming and diagnostics. It ships as a premium WPF EXE and includes PowerShell companions for quick checks, GUI-assisted tweaks and release launching.

Current app version: `1.2.0`.

Nokara does not promise magic FPS. It helps diagnose common issues, apply safe and visible changes with confirmation, keep logs/backups, and generate readable reports. Real results depend on hardware, Windows, drivers, game settings and background apps.

## Use modes

1. Premium WPF EXE: full Nokara System Manager desktop app.
2. PowerShell Lite: text-mode companion for quick diagnostics and safe maintenance.
3. PowerShell Tool GUI: standalone PowerShell GUI for selected safe tweaks and reports.
4. Launcher: downloads the latest portable ZIP from GitHub Releases and starts the EXE.

## Highlights

- WPF desktop app for Windows 10/11.
- Initial window sized for small screens, centered, resizable and using the native Windows title bar.
- Top tabs for Dashboard, Maintenance, Diagnostic, Gaming, Network, Streaming, Processes, Privacy, Backups and Settings.
- About screen opens from the Nokara brand button instead of a separate tab.
- Diagnostic cards for stuttering, RAM, storage, network, games and streaming context.
- Safe maintenance actions with admin detection and status feedback.
- DNS tools, process visibility, backups, action history and final reports.
- Visible pending-state labels for actions that are not implemented yet.
- Premium dark scrollbars for app content, tabs and DataGrid tables.
- PowerShell Lite menu, PowerShell Tool GUI and ZIP-based release launcher.

## EXE version

Download the portable ZIP from GitHub Releases. Binaries are not stored in the repository source tree.

Typical release asset:

```text
NokaraSystemManager-v1.2.0-win-x64-portable.zip
NokaraSystemManager-v1.2.0-win-x64-portable.zip.sha256
NokaraSystemManager.exe.sha256
```

Extract the ZIP and run:

```powershell
.\NokaraSystemManager.exe
```

The EXE requests administrator privileges because several features need an elevated context. The app still shows whether it is running as admin.

## PowerShell Lite

PowerShell Lite is a text-mode companion for quick diagnostics and safe maintenance:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1" | iex
```

It includes quick diagnostic, safe maintenance, Flush DNS, basic network repair with confirmation, heavy process review, stuttering checks, RAM diagnostics, gaming/streaming checklists, storage scan, safety limits, EXE launcher help and report export.

## PowerShell Tool GUI

The PowerShell Tool is a standalone GUI built with Windows Forms. It is useful when you want a lightweight graphical companion without opening the WPF EXE:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-tool.ps1" | iex
```

It groups actions into Overview, Tweaks, Gaming, Streaming, Network, Storage, Reports and About. Pending actions remain blocked and visible.

## PowerShell launcher

The launcher fetches the latest portable ZIP from GitHub Releases, optionally validates SHA256 assets, extracts it under `%LocalAppData%\NokaraSystemManager\bin`, and starts the EXE:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/launch.ps1" | iex
```

Repository owner, repository name, release tag, install root and logs are configurable variables or parameters.

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
- Tool logs: `%AppData%\NokaraSystemManager\ToolLogs`
- Launcher logs: `%AppData%\NokaraSystemManager\LauncherLogs`

Final reports are also written to the Desktop as text files. Review reports before sharing them because they can include machine name, user name, local IP, adapter names and process names.

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

Do not commit publish outputs, ZIP files or EXE binaries to the repository. Upload release binaries to GitHub Releases instead.

## Screenshots

Screenshots belong in `docs/screenshots/`. Use only real screenshots and avoid exposing local user names, machine names, IP addresses or private process lists.

Recommended screenshots:

- `dashboard.png`
- `diagnostic.png`
- `maintenance.png`
- `processes.png`
- `network.png`
- `powershell-tool.png`
- `final-report-redacted.png`

## GitHub releases

Suggested release assets:

- `NokaraSystemManager-v1.2.0-win-x64-portable.zip`
- `NokaraSystemManager-v1.2.0-win-x64-portable.zip.sha256`
- `NokaraSystemManager.exe.sha256`
- `RELEASE_NOTES_v1.2.0.md`

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
