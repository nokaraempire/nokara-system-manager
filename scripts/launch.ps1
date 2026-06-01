$ErrorActionPreference = "Stop"

# Nokara System Manager launcher
# Downloads the latest portable ZIP from GitHub Releases, verifies SHA256 if available,
# extracts it under LocalAppData and starts NokaraSystemManager.exe.

$RepoOwner = "nokaraempire"
$RepoName = "nokara-system-manager"

$InstallRoot = Join-Path $env:LOCALAPPDATA "NokaraSystemManager"
$InstallDir = Join-Path $InstallRoot "bin"
$DownloadDir = Join-Path $InstallRoot "downloads"
$LogDir = Join-Path $env:APPDATA "NokaraSystemManager\LauncherLogs"
$LogFile = Join-Path $LogDir ("launcher-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))

New-Item -ItemType Directory -Force -Path $InstallDir, $DownloadDir, $LogDir | Out-Null

function Write-LauncherLog {
    param(
        [string]$Level,
        [string]$Message
    )

    Add-Content -Path $LogFile -Value ("[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message)
}

function Write-LauncherStatus {
    param([string]$Message)

    Write-Host "[Nokara] $Message" -ForegroundColor Cyan
    Write-LauncherLog "INFO" $Message
}

function Write-LauncherWarning {
    param([string]$Message)

    Write-Host "[Nokara] $Message" -ForegroundColor Yellow
    Write-LauncherLog "WARN" $Message
}

function Write-LauncherError {
    param([string]$Message)

    Write-Host "[Nokara] $Message" -ForegroundColor Red
    Write-LauncherLog "ERROR" $Message
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)

    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

try {
    Write-LauncherStatus "Buscando latest release en GitHub..."

    $releaseUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
    $release = Invoke-RestMethod -Uri $releaseUrl -Headers @{
        "User-Agent" = "NokaraSystemManagerLauncher"
    }

    $zipAsset = $release.assets |
        Where-Object {
            $_.name -like "NokaraSystemManager-*-win-x64-portable.zip"
        } |
        Select-Object -First 1

    if (-not $zipAsset) {
        Write-LauncherError "No se encontro el ZIP portable en latest release."
        Write-LauncherWarning "Asset esperado: NokaraSystemManager-*-win-x64-portable.zip"
        return
    }

    $zipPath = Join-Path $DownloadDir $zipAsset.name
    $shaPath = "$zipPath.sha256"

    Write-LauncherStatus "Descargando $($zipAsset.name)..."
    Invoke-WebRequest -Uri $zipAsset.browser_download_url -OutFile $zipPath -UseBasicParsing

    $shaAsset = $release.assets |
        Where-Object {
            $_.name -eq "$($zipAsset.name).sha256"
        } |
        Select-Object -First 1

    if ($shaAsset) {
        Write-LauncherStatus "Descargando SHA256..."
        Invoke-WebRequest -Uri $shaAsset.browser_download_url -OutFile $shaPath -UseBasicParsing

        $expectedHash = (Get-Content $shaPath -Raw).Trim().Split(" ")[0].ToUpperInvariant()
        $actualHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()

        if ($expectedHash -ne $actualHash) {
            Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
            Write-LauncherError "Hash SHA256 invalido. No se ejecuta la app."
            Write-LauncherWarning "Esperado: $expectedHash"
            Write-LauncherWarning "Actual:   $actualHash"
            return
        }

        Write-LauncherStatus "Hash SHA256 del ZIP verificado."
    }
    else {
        Write-LauncherWarning "No hay SHA256 del ZIP publicado. Se continua sin verificacion opcional."
    }

    Write-LauncherStatus "Extrayendo portable..."

    if (Test-Path $InstallDir) {
        Remove-Item -Path $InstallDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $InstallDir -Force

    $exe = Get-ChildItem -Path $InstallDir -Recurse -Filter "NokaraSystemManager.exe" |
        Select-Object -First 1

    if (-not $exe) {
        Write-LauncherError "No se encontro NokaraSystemManager.exe dentro del ZIP extraido."
        return
    }

    $exePath = $exe.FullName

    $exeShaAsset = $release.assets |
        Where-Object {
            $_.name -eq "NokaraSystemManager.exe.sha256"
        } |
        Select-Object -First 1

    if ($exeShaAsset) {
        $exeShaPath = "$exePath.sha256"

        Write-LauncherStatus "Descargando SHA256 del EXE..."
        Invoke-WebRequest -Uri $exeShaAsset.browser_download_url -OutFile $exeShaPath -UseBasicParsing

        $expectedExeHash = (Get-Content $exeShaPath -Raw).Trim().Split(" ")[0].ToUpperInvariant()
        $actualExeHash = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash.ToUpperInvariant()

        if ($expectedExeHash -ne $actualExeHash) {
            Write-LauncherError "Hash SHA256 del EXE invalido. No se ejecuta la app."
            Write-LauncherWarning "Esperado: $expectedExeHash"
            Write-LauncherWarning "Actual:   $actualExeHash"
            return
        }

        Write-LauncherStatus "Hash SHA256 del EXE verificado."
    }
    else {
        Write-LauncherWarning "No hay SHA256 del EXE publicado. Se continua sin verificacion opcional."
    }

    Write-LauncherStatus "Ejecutando Nokara System Manager..."

    if (Test-IsAdmin) {
        Start-Process -FilePath $exePath
    }
    else {
        Write-LauncherStatus "Solicitando permisos de administrador para la app completa..."
        Start-Process -FilePath $exePath -Verb RunAs
    }
}
catch {
    Write-LauncherError $_.Exception.Message
    Write-LauncherWarning "No se pudo descargar/abrir la ultima version. Revisa conexion, release, assets y permisos."
}
