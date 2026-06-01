# Nokara System Manager

Windows maintenance, gaming, streaming and diagnostics toolkit with a premium WPF interface and a PowerShell Lite mode.

Nokara System Manager helps users review common Windows performance issues, run safe maintenance actions, inspect processes, generate reports and apply guided profiles for gaming or streaming setups.

It does **not** promise magic FPS gains. It focuses on diagnosis, safe actions, clear explanations, logs, backups and reversibility when possible.

[Latest release](https://github.com/nokaraempire/nokara-system-manager/releases/latest) · [Report a bug](https://github.com/nokaraempire/nokara-system-manager/issues/new/choose) · [Security](SECURITY.md) · [Changelog](CHANGELOG.md)

---

## Download

### Recommended: portable EXE

Download the latest portable build from GitHub Releases:

[Download latest release](https://github.com/nokaraempire/nokara-system-manager/releases/latest)

For version `v1.1.1`, download:

```text
NokaraSystemManager-v1.1.1-win-x64-portable.zip
```

Direct download:

```text
https://github.com/nokaraempire/nokara-system-manager/releases/download/v1.1.1/NokaraSystemManager-v1.1.1-win-x64-portable.zip
```

How to use it:

1. Download the `.zip`.
2. Extract it.
3. Open the extracted folder.
4. Run:

```powershell
.\NokaraSystemManager.exe
```

Some actions require administrator permissions. The app shows whether it is running as administrator and offers a restart-as-admin action when needed.

---

## Quick start

### Full EXE version

Use this if you want the full visual interface:

```text
1. Go to Releases.
2. Download NokaraSystemManager-v1.1.1-win-x64-portable.zip.
3. Extract the ZIP.
4. Run NokaraSystemManager.exe.
```

Release page:

```text
https://github.com/nokaraempire/nokara-system-manager/releases/latest
```

### PowerShell Lite

PowerShell Lite runs directly from PowerShell without installing the EXE:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/nokara-lite.ps1" | iex
```

It includes quick maintenance, Flush DNS, basic network repair with confirmation, heavy process review, stuttering checks, RAM diagnostics, gaming/streaming checklists, storage scan and report export.

### PowerShell launcher

The launcher downloads the latest release build and opens the EXE:

```powershell
irm "https://raw.githubusercontent.com/nokaraempire/nokara-system-manager/main/scripts/launch.ps1" | iex
```

The launcher uses GitHub Releases as the download source.

---

## Windows SmartScreen warning

When running `NokaraSystemManager.exe`, Windows may show this warning:

```text
Windows protected your PC
Microsoft Defender SmartScreen prevented an unrecognized app from starting.
Publisher: Unknown publisher
```

This warning can appear because Nokara System Manager is currently a new unsigned executable. Windows may warn about apps that do not yet have publisher reputation or enough download history.

This does **not automatically mean the app is malware**, but users should still be careful and only run files downloaded from the official Nokara Empire repository.

Official release page:

```text
https://github.com/nokaraempire/nokara-system-manager/releases/latest
```

To run the app after downloading it from the official release:

1. Click **More info**.
2. Confirm the app name is:

```text
NokaraSystemManager.exe
```

3. Confirm the publisher appears as:

```text
Unknown publisher
```

4. Click **Run anyway**.

Do **not** run reuploaded, modified or random fork versions unless you reviewed the code and trust the source.

Long term, this warning can be reduced by signing the executable with a code-signing certificate or distributing through a trusted store. For now, SHA256 hashes are provided in each release so users can verify file integrity.

---

## Verify SHA256

Release assets include SHA256 files:

```text
NokaraSystemManager-v1.1.1-win-x64-portable.zip.sha256
NokaraSystemManager.exe.sha256
```

To verify the downloaded ZIP with PowerShell:

```powershell
Get-FileHash .\NokaraSystemManager-v1.1.1-win-x64-portable.zip -Algorithm SHA256
```

Compare the result with the value inside:

```text
NokaraSystemManager-v1.1.1-win-x64-portable.zip.sha256
```

The EXE hash is included for the executable inside the extracted portable folder.

---

## Main features

### Premium EXE interface

* WPF desktop app for Windows.
* Dark premium UI.
* Browser-shell navigation with top tabs.
* Command/address bar.
* Basic and advanced modes.
* Admin status indicator.
* Logs, backups and reports.
* Guided action cards with risk labels and confirmations.

### Diagnostics

* General system status.
* Stuttering checks.
* RAM usage overview.
* Heavy process detection.
* Storage checks.
* Network information.
* Gaming and streaming readiness.
* Report generation.

### Gaming / FPS

* Game Mode checks.
* Game DVR / capture checks.
* Power plan guidance.
* Overlay and launcher detection.
* Wallpaper Engine detection.
* Heavy process review.
* Game-specific and global optimization paths.

### Streaming

* Streaming app detection.
* Discord/voice app detection.
* Browser and RAM impact review.
* Gaming + Streaming profile.
* Safe process-priority guidance.
* Avoids aggressive network resets during live streaming.

### Maintenance

* Temporary file cleanup.
* User TEMP cleanup.
* Windows TEMP analysis.
* DNS flush.
* Space analysis.
* Report export.

### Network / DNS

* Active adapter detection.
* Current DNS display.
* Flush DNS.
* DNS benchmark support.
* Safe network repair actions with confirmation.
* Advanced actions are clearly marked.

### Storage

* Disk detection.
* Free space overview.
* Safe scan flow.
* Uses safe scan logic by default.
* Does not run destructive repair commands automatically.

### Reports

Nokara can generate a final report on the Desktop and also store a copy in AppData.

The report is separated into sections such as:

* Summary.
* Screen / GPU.
* Hardware.
* Games.
* Streaming.
* RAM.
* Network.
* Storage.
* Applied changes.
* Problems found.
* Recommended next steps.

---

## Safety model

Nokara System Manager is intentionally conservative.

It does **not**:

* Disable Windows Defender.
* Disable Windows Firewall.
* Disable Windows Update.
* Touch anti-cheats.
* Use BCDEdit tweaks.
* Touch HPET.
* Apply MSI Mode tweaks.
* Modify drivers.
* Force Realtime process priority.
* Delete personal files.
* Run `chkdsk /f` automatically.
* Run `chkdsk /r` automatically.
* Close user processes without confirmation.
* Promise unrealistic FPS improvements.

Sensitive actions show explanations and confirmations before running.

---

## PowerShell security note

The commands using `irm | iex` download and execute a script from this repository.

You can review the scripts before running them:

* [scripts/nokara-lite.ps1](scripts/nokara-lite.ps1)
* [scripts/launch.ps1](scripts/launch.ps1)

If you do not trust remote execution, download the script manually, inspect it, then run it locally.

Example:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\nokara-lite.ps1
```

---

## Logs, backups and reports

Nokara stores runtime data under:

```text
%AppData%\NokaraSystemManager
```

Common folders:

```text
Logs
Backups
Config
Reports
LiteLogs
LauncherLogs
```

Generated reports are also copied to the Desktop for quick access.

---

## Project structure

```text
NokaraSystemManager
├─ Config/
├─ docs/
├─ Helpers/
├─ Models/
├─ scripts/
├─ Services/
├─ Styles/
├─ ViewModels/
├─ Views/
├─ App.xaml
├─ MainWindow.xaml
├─ NokaraSystemManager.csproj
├─ README.md
├─ SECURITY.md
├─ CHANGELOG.md
└─ LICENSE
```

Release binaries are not stored in the source tree. They are published through GitHub Releases.

---

## Build from source

Requirements:

* Windows.
* .NET SDK compatible with the project target framework.
* WPF support.

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run
```

Publish portable win-x64:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Published output is generated under:

```text
bin\Release\net10.0-windows\win-x64\publish
```

---

## GitHub Releases

Official releases are published here:

```text
https://github.com/nokaraempire/nokara-system-manager/releases/latest
```

Release assets for `v1.1.1`:

```text
NokaraSystemManager-v1.1.1-win-x64-portable.zip
NokaraSystemManager-v1.1.1-win-x64-portable.zip.sha256
NokaraSystemManager.exe.sha256
```

For normal users, download the portable ZIP. GitHub may also show automatic source code archives, but those are mainly for developers.

Do not commit publish outputs or EXE binaries to the repository source tree. Upload release binaries to GitHub Releases instead.

---

## Screenshots

Screenshots will be added after the first public visual pass.

Recommended screenshot files:

```text
docs/screenshots/dashboard.png
docs/screenshots/diagnostic.png
docs/screenshots/processes.png
docs/screenshots/maintenance.png
docs/screenshots/report.png
```

Recommended sections:

* Browser shell / Dashboard.
* Diagnostic.
* Processes.
* Maintenance.
* Final report.

<!--
When screenshots are available, enable this section:

![Dashboard](docs/screenshots/dashboard.png)
![Diagnostic](docs/screenshots/diagnostic.png)
![Processes](docs/screenshots/processes.png)
![Maintenance](docs/screenshots/maintenance.png)
-->

---

## Known limitations

* The EXE is currently unsigned, so SmartScreen warnings can appear.
* PowerShell launcher behavior depends on GitHub Releases being available.
* Some actions require administrator permissions.
* Storage repair actions are intentionally conservative.
* Advanced network actions can temporarily interrupt connectivity and require confirmation.
* Users should only download official releases from the Nokara Empire repository.

---

## Roadmap

Possible future improvements:

* Signed installer or signed portable executable.
* More complete screenshots and documentation.
* GitHub Actions build pipeline.
* Automatic release packaging.
* Better global search inside the EXE.
* More detailed game detection.
* More granular backup and restore flows.
* Improved PowerShell Lite UI.

---

## Credits

Created by **Nokara Empire**.

Developed by **kkshi / Fabián Almada**.

Nokara System Manager is a Windows maintenance, gaming, streaming and diagnostics toolkit focused on safe actions, transparency and practical reporting.

---

## License

This project is licensed under the MIT License.

See [LICENSE](LICENSE).
