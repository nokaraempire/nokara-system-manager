$ErrorActionPreference = "Stop"

# Nokara System Manager launcher.
# Downloads the latest EXE asset from GitHub Releases, optionally verifies a
# published SHA256 file, then starts the app. Keep these values configurable
# for forks or future repository moves. Do not hardcode local user paths here.
$RepoOwner = "nokaraempire"
$RepoName = "nokara-system-manager"
$AssetName = "NokaraSystemManager.exe"
$InstallDir = Join-Path $env:LOCALAPPDATA "NokaraSystemManager\bin"
$ExePath = Join-Path $InstallDir $AssetName
$LogDir = Join-Path $env:APPDATA "NokaraSystemManager\LauncherLogs"
$LogFile = Join-Path $LogDir ("launcher-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))

New-Item -ItemType Directory -Force -Path $InstallDir, $LogDir | Out-Null

function Write-LauncherLog {
    param([string]$Level, [string]$Message)
    Add-Content -Path $LogFile -Value ("[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message)
}

function Write-LauncherStatus { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Cyan; Write-LauncherLog "INFO" $Message }
function Write-LauncherWarning { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Yellow; Write-LauncherLog "WARN" $Message }
function Write-LauncherError { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Red; Write-LauncherLog "ERROR" $Message }

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

try {
    Write-LauncherStatus "Buscando latest release en GitHub..."
    $releaseUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
    $release = Invoke-RestMethod -Uri $releaseUrl -Headers @{ "User-Agent" = "NokaraSystemManagerLauncher" }
    $asset = $release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1

    if (-not $asset) {
        Write-LauncherWarning "No se encontro el asset $AssetName en latest release."
        Write-LauncherWarning "Si todavia no existe release real, publicalo en GitHub Releases y volve a ejecutar el launcher."
        return
    }

    Write-LauncherStatus "Descargando $AssetName..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $ExePath -UseBasicParsing

    $hashAsset = $release.assets | Where-Object { $_.name -eq "$AssetName.sha256" } | Select-Object -First 1
    if ($hashAsset) {
        $hashPath = "$ExePath.sha256"
        Invoke-WebRequest -Uri $hashAsset.browser_download_url -OutFile $hashPath -UseBasicParsing
        $expected = (Get-Content $hashPath -Raw).Trim().Split(" ")[0].ToUpperInvariant()
        $actual = (Get-FileHash -Path $ExePath -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($expected -ne $actual) {
            Remove-Item -Path $ExePath -Force -ErrorAction SilentlyContinue
            Write-LauncherError "Hash SHA256 invalido. No se ejecuta el EXE."
            return
        }
        Write-LauncherStatus "Hash SHA256 verificado."
    } else {
        Write-LauncherWarning "No hay hash .sha256 publicado. Se continua sin verificacion opcional."
    }

    Write-LauncherStatus "Ejecutando Nokara System Manager..."
    if (Test-IsAdmin) {
        Start-Process -FilePath $ExePath
    } else {
        Write-LauncherStatus "Solicitando permisos de administrador para la app completa..."
        Start-Process -FilePath $ExePath -Verb RunAs
    }
} catch {
    Write-LauncherError $_.Exception.Message
    Write-LauncherWarning "No se pudo descargar/abrir la ultima version. Revisa conexion, releases y permisos."
}
