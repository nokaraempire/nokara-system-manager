# Safety

Nokara System Manager is intentionally conservative. The project avoids common risky optimizer claims and actions.

## Not performed by Nokara

- No Defender disable.
- No Firewall disable.
- No Windows Update disable.
- No BCDEDIT tweaks.
- No HPET changes.
- No MSI Mode changes.
- No driver modifications.
- No anti-cheat modifications.
- No Realtime priority.
- No automatic process killing.
- No personal-file deletion.
- No external RAM-cleaner download.

## Storage checks

Storage checks use:

```powershell
chkdsk X: /scan
```

Nokara does not run `/f` or `/r` automatically. If errors are found, make a backup and repair through Windows using the appropriate workflow.

## Reversibility

Actions that modify DNS, selected registry-backed settings or power plan state should create backups when supported. Backups are stored in `%AppData%\NokaraSystemManager\Backups`.

## Support reports

Share generated reports only after reviewing them. Reports may include machine name, user name, adapter names, local IP and detected processes.
