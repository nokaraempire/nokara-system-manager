param([switch]$NoMenu)

# Nokara System Manager Lite.
# Safe text-mode companion for quick diagnostics and maintenance. This script
# uses only environment-based user paths and keeps public project URLs in
# variables so forks/releases can adjust them without editing functions.
$script:RepoOwner = "nokaraempire"
$script:RepoName = "nokara-system-manager"
$script:GitHubBaseUrl = "https://github.com/$script:RepoOwner"
$script:ProjectUrl = "$script:GitHubBaseUrl/$script:RepoName"
$script:RawBaseUrl = "https://raw.githubusercontent.com/$script:RepoOwner/$script:RepoName/main"
$script:IssuesUrl = "$script:ProjectUrl/issues"
$script:InstagramUrl = "https://instagram.com/nokaraempire"
$script:TikTokUrl = "https://tiktok.com/@nokaraempire"
$script:YouTubeUrl = "https://youtube.com/@nokaraempire"
$script:NokaraRoot = Join-Path $env:APPDATA "NokaraSystemManager"
$script:LiteLogDir = Join-Path $script:NokaraRoot "LiteLogs"
$script:ReportDir = Join-Path $script:NokaraRoot "Reports"
$script:LogFile = Join-Path $script:LiteLogDir ("nokara-lite-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))
$script:SupportsAnsi = $Host.Name -notmatch "ISE"
$script:Esc = [char]27

New-Item -ItemType Directory -Force -Path $script:LiteLogDir, $script:ReportDir | Out-Null

function Write-NokaraLog {
    param([string]$Level, [string]$Message)
    $line = "[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -Path $script:LogFile -Value $line
}

function Write-NokaraColor {
    param([string]$Text, [string]$Color = "Gray", [string]$Ansi = "37")
    if ($script:SupportsAnsi) {
        Write-Host ("{0}[{1}m{2}{0}[0m" -f $script:Esc, $Ansi, $Text)
    } else {
        Write-Host $Text -ForegroundColor $Color
    }
}

function Write-NokaraHeader {
    Clear-Host
    Write-NokaraColor "+--------------------------------------------------------------+" Cyan "36"
    Write-NokaraColor "|  NOKARA SYSTEM MANAGER LITE                                 |" Cyan "36"
    Write-NokaraColor "|  Safe Windows maintenance, gaming and streaming diagnostics |" Cyan "36"
    Write-NokaraColor "+--------------------------------------------------------------+" Cyan "36"
    Write-NokaraColor "No magic FPS. No Defender/Firewall/Windows Update changes. No BCDEDIT/HPET/MSI Mode." DarkGray "90"
    Write-Host ""
}

function Write-Panel {
    param([string]$Title, [string[]]$Lines)
    Write-NokaraColor (("+-- {0} " -f $Title).PadRight(64, "-") + "+") DarkCyan "36"
    foreach ($line in $Lines) {
        $text = if ($line.Length -gt 58) { $line.Substring(0, 58) } else { $line }
        Write-NokaraColor (("| {0}" -f $text).PadRight(64) + "|") Gray "37"
    }
    Write-NokaraColor ("+".PadRight(64, "-") + "+") DarkCyan "36"
}

function Write-Menu {
    Write-Panel "Menu" @(
        "1.  Mantenimiento rapido",
        "2.  Flush DNS",
        "3.  Reparacion basica de red",
        "4.  Procesos pesados",
        "5.  Diagnostico de stuttering",
        "6.  RAM / memoria",
        "7.  Checklist antes de jugar",
        "8.  Checklist antes de stream",
        "9.  Buscar errores de almacenamiento",
        "10. Exportar diagnostico",
        "11. Descargar/abrir version EXE",
        "12. Redes Nokara Empire",
        "13. Salir"
    )
}

function Write-Status { param([string]$Message) Write-NokaraColor "[i] $Message" Gray "37"; Write-NokaraLog "INFO" $Message }
function Write-Success { param([string]$Message) Write-NokaraColor "[OK] $Message" Green "32"; Write-NokaraLog "OK" $Message }
function Write-Warning { param([string]$Message) Write-NokaraColor "[!] $Message" Yellow "33"; Write-NokaraLog "WARN" $Message }
function Write-ErrorMessage { param([string]$Message) Write-NokaraColor "[X] $Message" Red "31"; Write-NokaraLog "ERROR" $Message }

function Confirm-NokaraAction {
    param([string]$Message, [switch]$Strong)
    Write-Warning $Message
    if ($Strong) {
        $answer = Read-Host "Escribi SI para continuar"
        return $answer -eq "SI"
    }
    $answer = Read-Host "Continuar? (s/N)"
    return $answer -match "^(s|si|y|yes)$"
}

function Pause-Nokara {
    Write-Host ""
    Read-Host "Enter para continuar" | Out-Null
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-NokaraSystemInfo {
    $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
    $cpu = Get-CimInstance Win32_Processor -ErrorAction SilentlyContinue | Select-Object -First 1
    $gpu = Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
    [pscustomobject]@{
        Computer = $env:COMPUTERNAME
        User = $env:USERNAME
        Windows = if ($os) { $os.Caption } else { "No detectado" }
        Admin = Test-IsAdmin
        Cpu = if ($cpu) { $cpu.Name } else { "No detectado" }
        RamGB = if ($os) { [math]::Round($os.TotalVisibleMemorySize / 1MB, 1) } else { 0 }
        FreeRamGB = if ($os) { [math]::Round($os.FreePhysicalMemory / 1MB, 1) } else { 0 }
        Gpu = if ($gpu) { ($gpu -join ", ") } else { "No detectado" }
    }
}

function Get-NokaraNetworkInfo {
    $adapter = Get-NetIPConfiguration -ErrorAction SilentlyContinue | Where-Object { $_.IPv4Address -and $_.NetAdapter.Status -eq "Up" } | Select-Object -First 1
    [pscustomobject]@{
        Adapter = if ($adapter) { $adapter.InterfaceAlias } else { "No detectado" }
        IP = if ($adapter) { $adapter.IPv4Address.IPAddress } else { "No detectado" }
        Gateway = if ($adapter -and $adapter.IPv4DefaultGateway) { $adapter.IPv4DefaultGateway.NextHop } else { "No detectado" }
        DNS = if ($adapter -and $adapter.DNSServer) { ($adapter.DNSServer.ServerAddresses -join ", ") } else { "No detectado" }
    }
}

function Get-NokaraHeavyProcesses {
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.WorkingSet64 -gt 500MB -and $_.ProcessName -notmatch "^(System|Registry|Idle|csrss|wininit|services|lsass)$" } |
        Sort-Object WorkingSet64 -Descending |
        Select-Object -First 15 @{n="Name";e={$_.ProcessName}}, Id, @{n="RAM_MB";e={[math]::Round($_.WorkingSet64 / 1MB, 1)}}
}

function Get-NokaraStorageInfo {
    Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" -ErrorAction SilentlyContinue |
        Select-Object DeviceID, VolumeName, @{n="FreeGB";e={[math]::Round($_.FreeSpace / 1GB, 1)}}, @{n="SizeGB";e={[math]::Round($_.Size / 1GB, 1)}}, @{n="FreePercent";e={if ($_.Size) {[math]::Round(($_.FreeSpace * 100) / $_.Size, 1)} else {0}}}
}

function Get-NokaraDetectedGames {
    $patterns = "steam|epic|riot|valorant|league|fortnite|cs2|csgo|battle.net|overwatch|eaapp|ubisoft|rockstar"
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessName -match $patterns } |
        Select-Object -First 20 ProcessName, Id
}

function Get-NokaraDetectedApps {
    $processes = Get-Process -ErrorAction SilentlyContinue
    [pscustomobject]@{
        Wallpaper = @($processes | Where-Object { $_.ProcessName -in @("wallpaper64","wallpaper32") } | Select-Object -ExpandProperty ProcessName)
        Streaming = @($processes | Where-Object { $_.ProcessName -match "obs|streamlabs|tiktok" } | Select-Object -ExpandProperty ProcessName)
        Voice = @($processes | Where-Object { $_.ProcessName -match "discord|teamspeak" } | Select-Object -ExpandProperty ProcessName)
        Browsers = @($processes | Where-Object { $_.ProcessName -match "chrome|msedge|firefox|brave|opera" } | Select-Object -ExpandProperty ProcessName -Unique)
        Overlays = @($processes | Where-Object { $_.ProcessName -match "discord|steam|gamebar|nvidia|radeon|medal|outplayed" } | Select-Object -ExpandProperty ProcessName -Unique)
    }
}

function Invoke-NokaraQuickMaintenance {
    Write-Status "Limpiando TEMP de usuario..."
    Get-ChildItem -Path $env:TEMP -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "TEMP de usuario procesado. Archivos en uso fueron omitidos."
    if (Test-IsAdmin) {
        if (Confirm-NokaraAction "Limpiar TEMP de Windows? Se omiten archivos en uso.") {
            Get-ChildItem -Path "$env:windir\Temp" -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            Write-Success "TEMP de Windows procesado."
        }
    } else {
        Write-Warning "TEMP de Windows requiere admin."
    }
}

function Invoke-NokaraFlushDns {
    ipconfig /flushdns | Out-Null
    Write-Success "Cache DNS limpiada."
}

function Invoke-NokaraBasicNetworkRepair {
    Invoke-NokaraFlushDns
    if (Confirm-NokaraAction "Release/Renew IP puede cortar la conexion unos segundos." -Strong) {
        ipconfig /release
        ipconfig /renew
        Write-Success "Release/Renew ejecutado."
    }
    if (Confirm-NokaraAction "Reset Winsock/TCP-IP puede requerir reinicio. No usar durante stream." -Strong) {
        netsh winsock reset
        netsh int ip reset
        Write-Warning "Reinicio recomendado."
    }
}

function Invoke-NokaraStutteringDiagnostic {
    $apps = Get-NokaraDetectedApps
    $heavy = Get-NokaraHeavyProcesses
    Write-Panel "Stuttering" @(
        "Wallpaper Engine: $($apps.Wallpaper -join ', ')",
        "Overlays: $($apps.Overlays -join ', ')",
        "Streaming: $($apps.Streaming -join ', ')",
        "Navegadores: $($apps.Browsers -join ', ')",
        "Procesos pesados: $(@($heavy).Count)"
    )
    Write-Warning "Sugerencia: revisar overlays y Wallpaper Engine manualmente. Nokara Lite no cierra procesos automaticamente."
}

function Invoke-NokaraMemoryDiagnostic {
    $sys = Get-NokaraSystemInfo
    $used = [math]::Max(0, $sys.RamGB - $sys.FreeRamGB)
    Write-Panel "RAM / memoria" @(
        "RAM total: $($sys.RamGB) GB",
        "RAM usada: $used GB",
        "RAM libre: $($sys.FreeRamGB) GB",
        "Liberar memoria no critica: pendiente sin herramientas externas"
    )
    Get-NokaraHeavyProcesses | Format-Table -AutoSize
}

function Invoke-NokaraGamingChecklist {
    $games = Get-NokaraDetectedGames
    $apps = Get-NokaraDetectedApps
    Write-Panel "Antes de jugar" @(
        "Juegos/launchers detectados: $(@($games).Count)",
        "Wallpaper Engine: $($apps.Wallpaper -join ', ')",
        "Overlays: $($apps.Overlays -join ', ')",
        "Procesos pesados: $(@(Get-NokaraHeavyProcesses).Count)",
        "No se toca anti-cheat, drivers, BCDEDIT, HPET ni MSI Mode"
    )
}

function Invoke-NokaraStreamingChecklist {
    $apps = Get-NokaraDetectedApps
    Write-Panel "Antes de stream" @(
        "Streaming: $($apps.Streaming -join ', ')",
        "Voz/Discord: $($apps.Voice -join ', ')",
        "Navegadores: $($apps.Browsers -join ', ')",
        "Red: no usar resets durante vivo",
        "Shader cache: no limpiar durante stream"
    )
}

function Invoke-NokaraStorageScan {
    $drives = Get-NokaraStorageInfo
    $drives | Format-Table -AutoSize
    $drive = Read-Host "Unidad para chkdsk /scan (ej: C:)"
    if ([string]::IsNullOrWhiteSpace($drive)) { return }
    if (Confirm-NokaraAction "Se ejecutara chkdsk $drive /scan. No se usara /f ni /r." -Strong) {
        chkdsk $drive /scan
        Write-Success "Comprobacion /scan finalizada."
    }
}

function Export-NokaraDiagnostic {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $desktop = [Environment]::GetFolderPath("DesktopDirectory")
    $path = Join-Path $desktop "Nokara-Diagnostic-$timestamp.txt"
    $copy = Join-Path $script:ReportDir "Nokara-Diagnostic-$timestamp.txt"
    $sys = Get-NokaraSystemInfo
    $net = Get-NokaraNetworkInfo
    $storage = Get-NokaraStorageInfo | Out-String
    $heavy = Get-NokaraHeavyProcesses | Out-String
    $apps = Get-NokaraDetectedApps | Format-List | Out-String
    $content = @"
Nokara System Manager Lite - Diagnostico
Fecha: $(Get-Date)

Resumen
Equipo: $($sys.Computer)
Usuario: $($sys.User)
Windows: $($sys.Windows)
Admin: $($sys.Admin)
CPU: $($sys.Cpu)
RAM: $($sys.FreeRamGB) GB libres de $($sys.RamGB) GB
GPU: $($sys.Gpu)

Red
Adaptador: $($net.Adapter)
IP: $($net.IP)
Gateway: $($net.Gateway)
DNS: $($net.DNS)

Almacenamiento
$storage

Procesos pesados
$heavy

Apps detectadas
$apps

Notas
Nokara System Manager no promete FPS magicos. No desactiva Defender, Firewall ni Windows Update.
No usa BCDEDIT, HPET, MSI Mode, drivers ni anti-cheats.
"@
    Set-Content -Path $path -Value $content -Encoding UTF8
    Set-Content -Path $copy -Value $content -Encoding UTF8
    Write-Success "Reporte creado: $path"
    Start-Process notepad.exe $path
}

function Show-NokaraSocialLinks {
    Write-Panel "Nokara Empire" @(
        "GitHub: $script:GitHubBaseUrl",
        "Project: $script:ProjectUrl",
        "Instagram: $script:InstagramUrl",
        "TikTok: $script:TikTokUrl",
        "YouTube: $script:YouTubeUrl",
        "Issues: $script:IssuesUrl"
    )
}

function Invoke-NokaraExeLauncher {
    $url = "$script:RawBaseUrl/scripts/launch.ps1"
    Write-Status "Para usar launcher EXE:"
    Write-NokaraColor "irm `"$url`" | iex" Cyan "36"
}

function Invoke-NokaraMenu {
    do {
        Write-NokaraHeader
        Write-Menu
        $choice = Read-Host "Elegir opcion"
        switch ($choice) {
            "1" { Invoke-NokaraQuickMaintenance; Pause-Nokara }
            "2" { Invoke-NokaraFlushDns; Pause-Nokara }
            "3" { Invoke-NokaraBasicNetworkRepair; Pause-Nokara }
            "4" { Get-NokaraHeavyProcesses | Format-Table -AutoSize; Pause-Nokara }
            "5" { Invoke-NokaraStutteringDiagnostic; Pause-Nokara }
            "6" { Invoke-NokaraMemoryDiagnostic; Pause-Nokara }
            "7" { Invoke-NokaraGamingChecklist; Pause-Nokara }
            "8" { Invoke-NokaraStreamingChecklist; Pause-Nokara }
            "9" { Invoke-NokaraStorageScan; Pause-Nokara }
            "10" { Export-NokaraDiagnostic; Pause-Nokara }
            "11" { Invoke-NokaraExeLauncher; Pause-Nokara }
            "12" { Show-NokaraSocialLinks; Pause-Nokara }
            "13" { Write-Success "Saliendo de Nokara Lite."; return }
            default { Write-Warning "Opcion invalida."; Start-Sleep -Milliseconds 900 }
        }
    } while ($true)
}

Write-NokaraLog "INFO" "Nokara Lite iniciado."
if (-not $NoMenu) {
    Invoke-NokaraMenu
}
