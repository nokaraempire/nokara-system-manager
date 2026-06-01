# Nokara System Manager v1.2.0

Version `1.2.0` prepares Nokara System Manager for a public GitHub Release with an admin-first EXE, cleaner desktop shell, PowerShell GUI companion and ZIP-based distribution.

## Summary

- Premium WPF EXE for the complete desktop experience.
- PowerShell Lite for terminal diagnostics and safe maintenance.
- PowerShell Tool GUI for selected tweaks, reports and quick checks.
- Launcher that downloads the portable ZIP from GitHub Releases.
- Release assets prepared outside the source tree.

## EXE premium

- Version metadata updated to `1.2.0`.
- EXE requests administrator privileges through `app.manifest`.
- Initial window remains 1100x680, centered, resizable and native-titlebar friendly.
- Top shell now shows real controls only: tabs, refresh, export report, admin status and restart-as-admin.
- About opens from the Nokara brand button.
- Mode toggle/search/back/forward/home were removed because they were not needed for the app workflow.
- Pending actions are blocked with visible reasons instead of fake apply buttons.

## PowerShell Lite

- Menu expanded for quick diagnostic, safe maintenance, DNS, network repair, processes, stuttering, RAM, gaming, streaming, storage, reports, safety limits, launcher and GUI tool.
- Admin state is visible.
- Strong confirmations remain for actions that can interrupt network or require caution.

## PowerShell Tool GUI

- New `scripts/nokara-tool.ps1`.
- Windows Forms GUI with Overview, Tweaks, Gaming, Streaming, Network, Storage, Reports and About.
- Selected actions can run in batches.
- Undo is shown only for reversible PowerShell actions.
- Pending actions stay visible but blocked.

## Diagnostics

- Full diagnostic remains focused on stuttering, RAM, storage, network, detected games, streaming apps, overlays and report export.
- Diagnostic card buttons now distinguish real executable actions from unavailable/pending actions.

## Stuttering

- Continues to highlight Wallpaper Engine, overlays, streaming apps, heavy background processes and power-plan context.
- Does not close processes automatically.

## RAM

- Shows memory pressure and top memory processes.
- No external RAM cleaner binaries are downloaded or executed.

## FPS per game/global

- Gaming profile remains conservative: Game Mode, Game DVR, power plan, transparencies and mouse acceleration where supported.
- No magic FPS claims.
- No driver, anti-cheat, HPET, BCDEDIT or MSI Mode changes.

## Storage

- Safe cleanup remains focused on TEMP, thumbnail cache, DirectX shader cache, recycle bin and old Nokara logs.
- Storage repair uses `chkdsk /scan` only.
- Windows Update cache cleanup remains pending until a safe backup/reversion flow is validated.

## Final report

- Reports continue to write to Desktop and `%AppData%\NokaraSystemManager\Reports`.
- Reports may include machine name, user name, local IP, adapter names and process names. Review before sharing.

## Browser shell

- The old browser-like command/address bar was replaced by an app-focused shell.
- Scrollbars remain dark, visible and usable for content, tabs and tables.

## Security

- Does not disable Defender.
- Does not disable Firewall.
- Does not disable Windows Update.
- Does not touch anti-cheats.
- Does not run BCDEDIT, HPET or MSI Mode tweaks.
- Does not delete personal files.

## Known pending items

- Windows Update cache cleanup.
- USB Selective Suspend automation.
- Startup apps manager with backups.
- Post-format guided profile.
- Code signing certificate for reducing SmartScreen friction.

## SmartScreen

Windows SmartScreen may warn because this is an unsigned or newly published binary. Download only from the official Nokara Empire GitHub Releases page and verify SHA256 assets when provided.
