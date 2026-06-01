# Usage

Nokara System Manager has four public entry points:

- Full WPF EXE for the complete desktop experience.
- PowerShell Lite for quick diagnostics and safe terminal maintenance.
- PowerShell Tool GUI for a lightweight graphical PowerShell experience.
- PowerShell launcher for downloading and running the latest GitHub Release ZIP.

## Full EXE

Use the ZIP from GitHub Releases. The EXE requests administrator privileges and shows admin status in the command bar and status bar.

Typical flow:

1. Open Dashboard.
2. Run Diagnostic to understand the system state.
3. Review risk, reversibility and admin requirements before applying an action.
4. Export a report when asking for support.
5. Use Backups to restore supported reversible changes.

## PowerShell Lite

PowerShell Lite is useful when the EXE is not available or when a quick text-mode report is enough. It writes logs and reports under `%AppData%\NokaraSystemManager`.

## PowerShell Tool GUI

PowerShell Tool GUI is useful for a compact GUI that can run from a raw GitHub one-liner. It does not replace the EXE; it is a lighter companion for selected actions and reports.

## Reports

Generated reports include Windows, hardware, RAM, storage, network, detected apps, actions and recommendations. Reports are intended for support and troubleshooting, not as a promise of performance improvement.

Review reports before sharing them because they may contain local user names, machine names, local IPs, adapter names and process names.
