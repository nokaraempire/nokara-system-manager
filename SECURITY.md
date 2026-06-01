# Security Policy

## Reporting issues

Report vulnerabilities, unsafe behavior or suspicious release assets here:

https://github.com/nokaraempire/nokara-system-manager/issues

If the issue includes private data, do not publish full logs or reports before reviewing them. Reports can include machine name, user name, local IP, network adapter names and process names.

## About `irm | iex`

PowerShell one-liners are convenient, but you should review scripts before executing them:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1"
```

Run scripts only from official Nokara Empire sources. Avoid unknown forks unless you audit the content.

## What Nokara does not do

Nokara System Manager does not disable Defender, Firewall or Windows Update. It does not touch anti-cheat systems such as Vanguard, EasyAntiCheat or BattlEye. It does not use BCDEDIT, HPET, MSI Mode or aggressive driver tweaks.

## Logs, backups and reports

Actions write logs. Reversible changes create backups when supported. Reports are saved in `%AppData%\NokaraSystemManager\Reports` and may also be copied to the Desktop.

## Releases and hashes

The launcher downloads `NokaraSystemManager.exe` from GitHub Releases. If `NokaraSystemManager.exe.sha256` is published next to the EXE, the launcher validates SHA256 before execution. If the hash fails, the EXE is deleted and not executed.

## SmartScreen

Windows SmartScreen may warn users when the binary is unsigned or newly released. This is expected for new public projects. Users should download only from the official GitHub Releases page.
