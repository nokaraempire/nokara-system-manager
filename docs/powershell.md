# PowerShell

This project includes three PowerShell entry points.

## Lite script

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1" | iex
```

Features:

- Quick diagnostic.
- Safe maintenance.
- Flush DNS.
- Basic network repair with confirmation.
- Heavy process review.
- Stuttering diagnostics.
- RAM diagnostics.
- Gaming and streaming checklists.
- Storage scan using `chkdsk /scan`.
- Safety limits.
- Diagnostic report export.
- Links to the EXE launcher and PowerShell Tool GUI.

Logs:

```text
%AppData%\NokaraSystemManager\LiteLogs
```

## PowerShell Tool GUI

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-tool.ps1" | iex
```

The GUI uses built-in Windows Forms. It groups actions by Overview, Tweaks, Gaming, Streaming, Network, Storage, Reports and About. It supports selected reversible actions, report export and admin relaunch.

Logs:

```text
%AppData%\NokaraSystemManager\ToolLogs
```

## EXE launcher

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/launch.ps1" | iex
```

The launcher downloads the latest portable ZIP from GitHub Releases, validates SHA256 assets when they exist, extracts under `%LocalAppData%\NokaraSystemManager\bin`, and starts the EXE with elevation.

Logs:

```text
%AppData%\NokaraSystemManager\LauncherLogs
```

## Configuration

Scripts keep repository owner/name, raw URL and local output paths configurable. Avoid hardcoding local user paths in scripts committed to the repository.
