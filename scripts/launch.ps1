param(
    [string]$RepoOwner = "nokaraempire",
    [string]$RepoName = "nokara-system-manager",
    [string]$ReleaseTag = "",
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA "NokaraSystemManager\bin")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

# Nokara System Manager launcher.
# Public installer/runner for the portable GitHub Release ZIP. It keeps every
# path configurable, stores files under LocalAppData, verifies SHA256 assets
# when published, then starts the WPF EXE with elevation.
$ZipAssetPattern = "^NokaraSystemManager-v?[0-9]+\.[0-9]+\.[0-9]+-win-x64-portable\.zip$"
$ExeName = "NokaraSystemManager.exe"
$LogDir = Join-Path $env:APPDATA "NokaraSystemManager\LauncherLogs"
$LogFile = Join-Path $LogDir ("launcher-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Force -Path $InstallRoot, $LogDir | Out-Null

function Write-LauncherLog {
    param([string]$Level, [string]$Message)
    Add-Content -Path $LogFile -Value ("[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message)
}

function Write-Status { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Cyan; Write-LauncherLog "INFO" $Message }
function Write-Warn { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Yellow; Write-LauncherLog "WARN" $Message }
function Write-Err { param([string]$Message) Write-Host "[Nokara] $Message" -ForegroundColor Red; Write-LauncherLog "ERROR" $Message }

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-ExpectedHash {
    param([string]$Text)
    $candidate = ($Text -split "\s+")[0].Trim()
    if ($candidate -match "^[A-Fa-f0-9]{64}$") {
        return $candidate.ToUpperInvariant()
    }
    return ""
}

function Invoke-DownloadFile {
    param([string]$Url, [string]$OutFile)
    Invoke-WebRequest -Uri $Url -OutFile $OutFile -UseBasicParsing -Headers @{ "User-Agent" = "NokaraSystemManagerLauncher" }
}

function Get-ReleaseInfo {
    $base = "https://api.github.com/repos/$RepoOwner/$RepoName/releases"
    $url = if ([string]::IsNullOrWhiteSpace($ReleaseTag)) { "$base/latest" } else { "$base/tags/$ReleaseTag" }
    Write-Status "Buscando release en GitHub: $url"
    return Invoke-RestMethod -Uri $url -Headers @{ "User-Agent" = "NokaraSystemManagerLauncher" }
}

try {
    $release = Get-ReleaseInfo
    $zipAsset = @($release.assets) |
        Where-Object { $_.name -match $ZipAssetPattern } |
        Sort-Object name -Descending |
        Select-Object -First 1

    if (-not $zipAsset) {
        $zipAsset = @($release.assets) |
            Where-Object { $_.name -like "*portable*.zip" -and $_.name -like "*win-x64*" } |
            Select-Object -First 1
    }

    if (-not $zipAsset) {
        Write-Warn "No se encontro un ZIP portable win-x64 en la release $($release.tag_name)."
        Write-Warn "Publica NokaraSystemManager-vX.Y.Z-win-x64-portable.zip en GitHub Releases y vuelve a ejecutar."
        return
    }

    $safeTag = ($release.tag_name -replace "[^A-Za-z0-9_.-]", "_")
    if ([string]::IsNullOrWhiteSpace($safeTag)) { $safeTag = "latest" }
    $downloadDir = Join-Path $InstallRoot "_downloads"
    $extractDir = Join-Path $InstallRoot $safeTag
    $zipPath = Join-Path $downloadDir $zipAsset.name
    New-Item -ItemType Directory -Force -Path $downloadDir | Out-Null

    Write-Status "Descargando $($zipAsset.name)..."
    Invoke-DownloadFile -Url $zipAsset.browser_download_url -OutFile $zipPath

    $hashAsset = @($release.assets) |
        Where-Object { $_.name -eq "$($zipAsset.name).sha256" -or $_.name -like "*.sha256" } |
        Select-Object -First 1

    if ($hashAsset) {
        $hashPath = "$zipPath.sha256"
        Invoke-DownloadFile -Url $hashAsset.browser_download_url -OutFile $hashPath
        $expected = Get-ExpectedHash -Text (Get-Content -Raw -Path $hashPath)
        $actual = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($expected -and $expected -ne $actual) {
            Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
            Write-Err "Hash SHA256 invalido para el ZIP. No se instala ni ejecuta."
            return
        }
        Write-Status "SHA256 del ZIP verificado."
    } else {
        Write-Warn "La release no incluye .sha256 para el ZIP. Se continua sin verificacion opcional."
    }

    if (Test-Path $extractDir) {
        $resolvedRoot = (Resolve-Path -LiteralPath $InstallRoot).Path
        $resolvedExtractParent = (Resolve-Path -LiteralPath (Split-Path -Parent $extractDir)).Path
        if ($resolvedExtractParent -eq $resolvedRoot) {
            Remove-Item -LiteralPath $extractDir -Recurse -Force
        }
    }

    New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
    Write-Status "Extrayendo portable en $extractDir"
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force

    $exePath = Get-ChildItem -Path $extractDir -Filter $ExeName -Recurse -File | Select-Object -First 1 -ExpandProperty FullName
    if (-not $exePath) {
        Write-Err "No se encontro $ExeName dentro del ZIP portable."
        return
    }

    $exeHashAsset = @($release.assets) | Where-Object { $_.name -eq "$ExeName.sha256" } | Select-Object -First 1
    if ($exeHashAsset) {
        $exeHashPath = "$exePath.sha256"
        Invoke-DownloadFile -Url $exeHashAsset.browser_download_url -OutFile $exeHashPath
        $expectedExe = Get-ExpectedHash -Text (Get-Content -Raw -Path $exeHashPath)
        $actualExe = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($expectedExe -and $expectedExe -ne $actualExe) {
            Write-Err "Hash SHA256 invalido para el EXE. No se ejecuta."
            return
        }
        Write-Status "SHA256 del EXE verificado."
    }

    Write-Status "Ejecutando Nokara System Manager..."
    if (Test-IsAdmin) {
        Start-Process -FilePath $exePath
    } else {
        Start-Process -FilePath $exePath -Verb RunAs
    }
} catch {
    Write-Err $_.Exception.Message
    Write-Warn "No se pudo descargar o abrir la ultima version. Revisa conexion, GitHub Releases y permisos."
}
