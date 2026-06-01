# Nokara System Manager v1.1.1

## Summary

Version `1.1.1` prepares Nokara System Manager for a public GitHub release with a more professional desktop shell, stronger diagnostics, safer documentation and release-ready PowerShell tooling.

This release keeps the project conservative: no magic FPS claims, no dangerous Windows tweaks, and no automatic destructive repair actions.

## New and improved

### Premium EXE

- WPF desktop app targeting `net10.0-windows`.
- Portable single-file publish supported for Windows x64.
- Native Windows titlebar preserved.
- Dark premium UI with browser-style shell.
- Top tabs for Dashboard, Maintenance, Diagnostic, Gaming, Network, Streaming, Processes, Privacy, Backups, Settings and About.

### Browser shell

- New tabbed app shell inspired by a modern browser.
- Command/address bar with `nokara://...` section routes.
- Back, forward, home and refresh controls.
- Local search suggestions for common actions.
- Better horizontal space for cards and DataGrid-heavy views.
- More visible desktop scrollbars, including the tab strip.

### PowerShell Lite

- Text-mode companion script for quick support and diagnostics.
- Safe menu for maintenance, DNS, basic network repair, heavy processes, stuttering, RAM, gaming, streaming, storage and report export.
- Uses `%AppData%\NokaraSystemManager` for logs and reports.
- No hardcoded local user paths.

### PowerShell launcher

- Downloads `NokaraSystemManager.exe` from the latest GitHub Release.
- Supports optional `.sha256` verification.
- Stores the EXE under `%LocalAppData%\NokaraSystemManager\bin`.
- Starts the app normally or requests admin through `RunAs` when needed.

## Diagnostics

### Stuttering

- Checks common stuttering causes such as overlays, Wallpaper Engine, Game DVR context, heavy browsers, launchers, streaming apps, free disk space and RAM pressure.
- Explains detected causes and suggests safe next steps.

### RAM

- Shows RAM usage and top processes by memory.
- Flags browsers, streaming tools, launchers and heavy apps.
- Keeps non-critical memory cleanup disabled until it can be implemented safely without external tools.

### FPS per game / global

- Focuses on safe global gaming checks and reversible actions.
- Helps review Game Mode, Game DVR, power plan, overlays and selected process priority.
- Does not edit game files blindly.

### Storage

- Lists detected drives and free space.
- Allows safe `chkdsk X: /scan` only after confirmation.
- Does not run `/f` or `/r` automatically.

### Final report

- Generates readable TXT reports for support and troubleshooting.
- Saves reports to Desktop and `%AppData%\NokaraSystemManager\Reports`.
- Includes system, RAM, storage, network, detected apps, actions and recommendations.

## Safety

Nokara System Manager does not:

- Disable Defender.
- Disable Firewall.
- Disable Windows Update.
- Use BCDEDIT.
- Change HPET.
- Apply MSI Mode.
- Modify drivers.
- Touch anti-cheat systems.
- Use Realtime process priority.
- Close processes without confirmation.
- Delete personal files.
- Download external RAM-cleaner binaries.

## Known pending items

- Real GitHub Release assets must be published before `launch.ps1` can download the EXE.
- `.sha256` verification requires publishing `NokaraSystemManager.exe.sha256` next to the EXE.
- Some advanced actions remain disabled until backup and rollback behavior is fully validated.
- Screenshots should be added under `docs/screenshots/` before wider public promotion.
- The EXE is not code-signed yet.

## SmartScreen notice

Windows SmartScreen may warn users because the EXE is new, unsigned, or not yet widely downloaded. Download only from the official Nokara Empire GitHub Releases page. If code signing is added later, this warning should become less common over time.

## Suggested release assets

- `NokaraSystemManager.exe`
- `NokaraSystemManager.exe.sha256`
- `RELEASE_NOTES_v1.1.1.md`
