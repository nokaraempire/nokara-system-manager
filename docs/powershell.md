# PowerShell

This project includes two PowerShell entry points.

## Lite script

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1" | iex
```

Features:

- Quick maintenance.
- Flush DNS.
- Basic network repair with confirmation.
- Heavy process review.
- Stuttering diagnostics.
- RAM diagnostics.
- Gaming and streaming checklists.
- Storage scan using `chkdsk /scan`.
- Diagnostic report export.

Logs:

```text
%AppData%\NokaraSystemManager\LiteLogs
```

## EXE launcher

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/launch.ps1" | iex
```

The launcher downloads the latest release asset, optionally validates a `.sha256` file, stores the EXE under `%LocalAppData%\NokaraSystemManager\bin`, and starts it. If the app needs admin rights, the launcher uses `RunAs`.

Logs:

```text
%AppData%\NokaraSystemManager\LauncherLogs
```

## Configuration

Both scripts keep configurable values at the top of the file. Avoid hardcoding local user paths in scripts committed to the repository.
