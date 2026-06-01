# Usage

Nokara System Manager has two supported entry points:

- Full WPF EXE for the complete desktop experience.
- PowerShell Lite for quick diagnostics and safe maintenance from a terminal.

## Full EXE

Use the EXE from GitHub Releases. Some actions require administrator privileges; the app shows admin status in the command bar and status bar.

Typical flow:

1. Open Dashboard.
2. Run Diagnostic to understand the system state.
3. Review risk, reversibility and admin requirements before applying an action.
4. Export a report when asking for support.
5. Use Backups to restore supported reversible changes.

## PowerShell Lite

PowerShell Lite is useful when the EXE is not available or when a quick text-mode report is enough. It writes logs and reports under `%AppData%\NokaraSystemManager`.

## Reports

Generated reports include Windows, hardware, RAM, storage, network, detected apps, actions and recommendations. They are intended for support and troubleshooting, not as a promise of performance improvement.
