param([switch]$NoGui)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

# Nokara Tool is a self-contained PowerShell GUI companion for the WPF EXE.
# It intentionally uses only built-in Windows PowerShell / .NET Framework APIs
# so it can run from a local file or through: irm <raw-url> | iex
$script:RepoOwner = "nokaraempire"
$script:RepoName = "nokara-system-manager"
$script:ProjectUrl = "https://github.com/$script:RepoOwner/$script:RepoName"
$script:RawBaseUrl = "https://raw.githubusercontent.com/$script:RepoOwner/$script:RepoName/main"
$script:Root = Join-Path $env:APPDATA "NokaraSystemManager"
$script:ReportDir = Join-Path $script:Root "Reports"
$script:LogDir = Join-Path $script:Root "ToolLogs"
$script:LogFile = Join-Path $script:LogDir ("nokara-tool-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))
$script:ActionControls = New-Object System.Collections.ArrayList
$script:AppliedActions = New-Object System.Collections.ArrayList
$script:LogBox = $null
$script:OverviewBox = $null

New-Item -ItemType Directory -Force -Path $script:ReportDir, $script:LogDir | Out-Null

function Test-NokaraAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-NokaraToolLog {
    param([string]$Level, [string]$Message)
    $line = "[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Add-Content -Path $script:LogFile -Value $line
    if ($script:LogBox) {
        $script:LogBox.AppendText($line + [Environment]::NewLine)
        $script:LogBox.SelectionStart = $script:LogBox.Text.Length
        $script:LogBox.ScrollToCaret()
    }
}

function Confirm-NokaraGui {
    param([string]$Title, [string]$Message, [switch]$Strong)
    $icon = if ($Strong) { [System.Windows.Forms.MessageBoxIcon]::Warning } else { [System.Windows.Forms.MessageBoxIcon]::Question }
    $result = [System.Windows.Forms.MessageBox]::Show($Message, $Title, [System.Windows.Forms.MessageBoxButtons]::YesNo, $icon)
    return $result -eq [System.Windows.Forms.DialogResult]::Yes
}

function Set-RegistryDword {
    param([string]$Path, [string]$Name, [int]$Value)
    if (-not (Test-Path $Path)) { New-Item -Path $Path -Force | Out-Null }
    New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType DWord -Force | Out-Null
}

function Remove-RegistryValueSafe {
    param([string]$Path, [string]$Name)
    if (Test-Path $Path) {
        Remove-ItemProperty -Path $Path -Name $Name -ErrorAction SilentlyContinue
    }
}

function Clear-PathContents {
    param([string]$Path, [string]$Pattern = "*")
    if (-not (Test-Path $Path)) { return 0 }
    $freed = 0L
    Get-ChildItem -Path $Path -Filter $Pattern -Force -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $len = if (-not $_.PSIsContainer) { $_.Length } else { 0 }
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            $freed += $len
        } catch { }
    }
    return $freed
}

function New-NokaraAction {
    param(
        [string]$Id,
        [string]$Module,
        [string]$Title,
        [string]$Description,
        [ValidateSet("Low","Medium","High")][string]$Risk = "Low",
        [switch]$RequiresAdmin,
        [switch]$IsReversible,
        [switch]$IsAvailable,
        [scriptblock]$Apply,
        [scriptblock]$Undo
    )
    [pscustomobject]@{
        Id = $Id
        Module = $Module
        Title = $Title
        Description = $Description
        Risk = $Risk
        RequiresAdmin = [bool]$RequiresAdmin
        IsReversible = [bool]$IsReversible
        IsAvailable = [bool]$IsAvailable
        Apply = $Apply
        Undo = $Undo
    }
}

function Get-NokaraActions {
    $actions = New-Object System.Collections.ArrayList

    $null = $actions.Add((New-NokaraAction -Id "maintenance.flushdns" -Module "Mantenimiento" -Title "Flush DNS" -Description "Limpia cache DNS local sin cambiar configuracion." -Risk Low -IsAvailable -Apply { ipconfig /flushdns | Out-Null; "Cache DNS limpiada." }))
    $null = $actions.Add((New-NokaraAction -Id "maintenance.user-temp" -Module "Mantenimiento" -Title "Limpiar TEMP usuario" -Description "Borra temporales del usuario; omite archivos en uso." -Risk Low -IsAvailable -Apply { $bytes = Clear-PathContents -Path $env:TEMP; "TEMP usuario procesado. Bytes directos liberados: $bytes" }))
    $null = $actions.Add((New-NokaraAction -Id "maintenance.windows-temp" -Module "Mantenimiento" -Title "Limpiar TEMP Windows" -Description "Borra temporales de Windows que no esten en uso." -Risk Medium -RequiresAdmin -IsAvailable -Apply { $bytes = Clear-PathContents -Path (Join-Path $env:windir "Temp"); "TEMP Windows procesado. Bytes directos liberados: $bytes" }))
    $null = $actions.Add((New-NokaraAction -Id "maintenance.thumbnails" -Module "Mantenimiento" -Title "Limpiar miniaturas" -Description "Elimina cache de miniaturas reconstruible." -Risk Low -IsAvailable -Apply { $path = Join-Path $env:LOCALAPPDATA "Microsoft\Windows\Explorer"; $bytes = Clear-PathContents -Path $path -Pattern "thumbcache_*.db"; "Cache de miniaturas procesada. Bytes directos liberados: $bytes" }))
    $null = $actions.Add((New-NokaraAction -Id "maintenance.dx-cache" -Module "Mantenimiento" -Title "Limpiar DirectX Shader Cache" -Description "Puede causar microcarga inicial al abrir juegos de nuevo." -Risk Low -IsAvailable -Apply { $path = Join-Path $env:LOCALAPPDATA "D3DSCache"; $bytes = Clear-PathContents -Path $path; "DirectX Shader Cache procesada. Bytes directos liberados: $bytes" }))

    $null = $actions.Add((New-NokaraAction -Id "gaming.game-mode" -Module "Gaming" -Title "Activar Game Mode" -Description "Habilita Game Mode en HKCU." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\Software\Microsoft\GameBar" -Name "AutoGameModeEnabled" -Value 1; "Game Mode activado." } -Undo { Set-RegistryDword -Path "HKCU:\Software\Microsoft\GameBar" -Name "AutoGameModeEnabled" -Value 0; "Game Mode revertido." }))
    $null = $actions.Add((New-NokaraAction -Id "gaming.game-dvr" -Module "Gaming" -Title "Desactivar Game DVR" -Description "Reduce capturas y grabacion de Xbox/Game Bar." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_Enabled" -Value 0; Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR" -Name "AppCaptureEnabled" -Value 0; "Game DVR desactivado." } -Undo { Set-RegistryDword -Path "HKCU:\System\GameConfigStore" -Name "GameDVR_Enabled" -Value 1; Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR" -Name "AppCaptureEnabled" -Value 1; "Game DVR revertido." }))
    $null = $actions.Add((New-NokaraAction -Id "gaming.high-performance" -Module "Gaming" -Title "Alto rendimiento" -Description "Activa el plan Alto rendimiento si Windows lo expone." -Risk Medium -IsAvailable -Apply { powercfg /setactive SCHEME_MIN | Out-Null; "Plan Alto rendimiento solicitado." }))
    $null = $actions.Add((New-NokaraAction -Id "gaming.transparency" -Module "Gaming" -Title "Reducir transparencias" -Description "Desactiva transparencias de Windows en HKCU." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Name "EnableTransparency" -Value 0; "Transparencias reducidas." } -Undo { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" -Name "EnableTransparency" -Value 1; "Transparencias restauradas." }))
    $null = $actions.Add((New-NokaraAction -Id "gaming.usb-suspend" -Module "Gaming" -Title "USB Selective Suspend" -Description "Pendiente: requiere backup completo por plan de energia." -Risk High -RequiresAdmin -Apply { "Pendiente en v1.2.0: no se aplica." }))

    $null = $actions.Add((New-NokaraAction -Id "streaming.snapshot" -Module "Streaming" -Title "Snapshot pre-stream" -Description "Solo lectura: apps de stream, voz, navegador y procesos pesados." -Risk Low -IsAvailable -Apply { Get-NokaraSnapshot | Out-String }))
    $null = $actions.Add((New-NokaraAction -Id "streaming.no-tips" -Module "Streaming" -Title "Reducir tips de Windows" -Description "Evita sugerencias durante capturas." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name "SubscribedContent-338389Enabled" -Value 0; "Tips reducidos." } -Undo { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name "SubscribedContent-338389Enabled" -Value 1; "Tips restaurados." }))

    $null = $actions.Add((New-NokaraAction -Id "network.release-renew" -Module "Red" -Title "Release/Renew IP" -Description "Puede cortar internet unos segundos." -Risk Medium -IsAvailable -Apply { ipconfig /release | Out-Null; ipconfig /renew | Out-Null; "Release/Renew ejecutado." }))
    $null = $actions.Add((New-NokaraAction -Id "network.winsock" -Module "Red" -Title "Reset Winsock/TCP-IP" -Description "Requiere reinicio recomendado." -Risk High -RequiresAdmin -IsAvailable -Apply { netsh winsock reset | Out-Null; netsh int ip reset | Out-Null; "Winsock/TCP-IP reseteado. Reinicio recomendado." }))

    $null = $actions.Add((New-NokaraAction -Id "config.file-ext" -Module "Config" -Title "Mostrar extensiones" -Description "Muestra extensiones conocidas en Explorer." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -Value 0; "Extensiones visibles." } -Undo { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -Value 1; "Extensiones ocultas por defecto." }))
    $null = $actions.Add((New-NokaraAction -Id "config.hidden-files" -Module "Config" -Title "Mostrar archivos ocultos" -Description "Muestra archivos ocultos no protegidos." -Risk Low -IsAvailable -IsReversible -Apply { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Hidden" -Value 1; "Archivos ocultos visibles." } -Undo { Set-RegistryDword -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Hidden" -Value 2; "Archivos ocultos restaurados." }))
    $null = $actions.Add((New-NokaraAction -Id "storage.scan" -Module "Storage" -Title "chkdsk /scan" -Description "Solo analiza. No usa /f ni /r." -Risk Medium -IsAvailable -Apply { $drive = [System.Environment]::GetEnvironmentVariable("SystemDrive"); chkdsk $drive /scan | Out-String }))

    return $actions
}

function Get-NokaraSnapshot {
    $os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
    $cpu = Get-CimInstance Win32_Processor -ErrorAction SilentlyContinue | Select-Object -First 1
    $heavy = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.WorkingSet64 -gt 500MB } | Sort-Object WorkingSet64 -Descending | Select-Object -First 8 ProcessName, Id, @{n="RAM_MB";e={[math]::Round($_.WorkingSet64 / 1MB, 1)}}
    $adapter = Get-NetIPConfiguration -ErrorAction SilentlyContinue | Where-Object { $_.IPv4Address -and $_.NetAdapter.Status -eq "Up" } | Select-Object -First 1
    [pscustomobject]@{
        Time = Get-Date
        Computer = $env:COMPUTERNAME
        User = $env:USERNAME
        Admin = Test-NokaraAdmin
        Windows = if ($os) { $os.Caption } else { "No detectado" }
        Cpu = if ($cpu) { $cpu.Name } else { "No detectado" }
        RamGB = if ($os) { [math]::Round($os.TotalVisibleMemorySize / 1MB, 1) } else { 0 }
        FreeRamGB = if ($os) { [math]::Round($os.FreePhysicalMemory / 1MB, 1) } else { 0 }
        Adapter = if ($adapter) { $adapter.InterfaceAlias } else { "No detectado" }
        Ip = if ($adapter) { $adapter.IPv4Address.IPAddress } else { "No detectado" }
        Dns = if ($adapter -and $adapter.DNSServer) { $adapter.DNSServer.ServerAddresses -join ", " } else { "No detectado" }
        HeavyProcesses = ($heavy | Out-String).Trim()
    }
}

function Get-CheckedActions {
    $selected = New-Object System.Collections.ArrayList
    foreach ($item in $script:ActionControls) {
        if ($item.CheckBox.Checked) {
            $null = $selected.Add($item.Action)
        }
    }
    return $selected
}

function Invoke-NokaraAction {
    param([object]$Action, [switch]$Undo)

    if ($Action.RequiresAdmin -and -not (Test-NokaraAdmin)) {
        Write-NokaraToolLog "WARN" "$($Action.Title) requiere administrador."
        return
    }

    if (-not $Action.IsAvailable) {
        Write-NokaraToolLog "WARN" "$($Action.Title) esta pendiente y no se ejecuta."
        return
    }

    if ($Undo -and -not $Action.IsReversible) {
        Write-NokaraToolLog "WARN" "$($Action.Title) no tiene undo seguro en PowerShell Tool."
        return
    }

    if ($Action.Risk -ne "Low") {
        $ok = Confirm-NokaraGui -Title "Confirmar accion" -Message "$($Action.Title)`n`nRiesgo: $($Action.Risk)`n$($Action.Description)`n`nContinuar?" -Strong
        if (-not $ok) {
            Write-NokaraToolLog "INFO" "$($Action.Title) cancelada."
            return
        }
    }

    try {
        $block = if ($Undo) { $Action.Undo } else { $Action.Apply }
        if (-not $block) {
            Write-NokaraToolLog "WARN" "$($Action.Title) no tiene operacion definida."
            return
        }

        $result = & $block
        $verb = if ($Undo) { "UNDO" } else { "OK" }
        Write-NokaraToolLog $verb "$($Action.Title): $result"
        if (-not $Undo) {
            $null = $script:AppliedActions.Add($Action)
        }
    } catch {
        Write-NokaraToolLog "ERROR" "$($Action.Title): $($_.Exception.Message)"
    }
}

function Export-NokaraToolReport {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $desktop = [Environment]::GetFolderPath("DesktopDirectory")
    $path = Join-Path $desktop "Nokara-Tool-Report-$timestamp.txt"
    $copy = Join-Path $script:ReportDir "Nokara-Tool-Report-$timestamp.txt"
    $snapshot = Get-NokaraSnapshot | Format-List | Out-String
    $content = @"
Nokara System Manager PowerShell Tool Report
Fecha: $(Get-Date)

Snapshot
$snapshot

Acciones aplicadas en esta sesion
$($script:AppliedActions | Select-Object Module, Title, Risk, IsReversible | Format-Table -AutoSize | Out-String)

Notas de seguridad
No se desactiva Defender, Firewall, Windows Update, drivers, anticheats, BCDEDIT, HPET ni MSI Mode.
"@
    Set-Content -Path $path -Value $content -Encoding UTF8
    Set-Content -Path $copy -Value $content -Encoding UTF8
    Write-NokaraToolLog "OK" "Reporte creado: $path"
    Start-Process notepad.exe $path
}

function Restart-NokaraToolAsAdmin {
    if ($PSCommandPath) {
        Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    } else {
        $url = "$script:RawBaseUrl/scripts/nokara-tool.ps1"
        Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"irm '$url' | iex`""
    }
}

function New-DarkLabel {
    param([string]$Text, [int]$Size = 9, [switch]$Bold)
    $label = New-Object System.Windows.Forms.Label
    $label.Text = $Text
    $label.ForeColor = [Drawing.Color]::FromArgb(242,242,245)
    $label.Font = New-Object Drawing.Font("Segoe UI", $Size, $(if ($Bold) { [Drawing.FontStyle]::Bold } else { [Drawing.FontStyle]::Regular }))
    $label.AutoSize = $true
    return $label
}

function Add-ActionCard {
    param([System.Windows.Forms.FlowLayoutPanel]$Panel, [object]$Action)
    $card = New-Object System.Windows.Forms.Panel
    $card.Width = 330
    $card.Height = 118
    $card.Margin = New-Object System.Windows.Forms.Padding(8)
    $card.BackColor = [Drawing.Color]::FromArgb(27,28,35)
    $card.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle

    $check = New-Object System.Windows.Forms.CheckBox
    $check.Text = $Action.Title
    $check.ForeColor = [Drawing.Color]::FromArgb(242,242,245)
    $check.Font = New-Object Drawing.Font("Segoe UI", 9, [Drawing.FontStyle]::Bold)
    $check.Left = 10
    $check.Top = 8
    $check.Width = 300
    $check.Enabled = $Action.IsAvailable

    $desc = New-DarkLabel -Text $Action.Description -Size 8
    $desc.Left = 12
    $desc.Top = 34
    $desc.Width = 300
    $desc.Height = 36
    $desc.AutoSize = $false

    $meta = New-DarkLabel -Text ("{0} | Riesgo: {1}{2}{3}" -f $Action.Module, $Action.Risk, $(if ($Action.RequiresAdmin) { " | Admin" } else { "" }), $(if ($Action.IsReversible) { " | Undo" } else { "" })) -Size 8
    $meta.ForeColor = if ($Action.IsAvailable) { [Drawing.Color]::FromArgb(168,168,179) } else { [Drawing.Color]::FromArgb(245,158,11) }
    $meta.Left = 12
    $meta.Top = 82

    $card.Controls.AddRange(@($check, $desc, $meta))
    $Panel.Controls.Add($card)
    $null = $script:ActionControls.Add([pscustomobject]@{ CheckBox = $check; Action = $Action })
}

function Add-ActionTab {
    param([System.Windows.Forms.TabControl]$Tabs, [string]$Name, [object[]]$Actions)
    $tab = New-Object System.Windows.Forms.TabPage
    $tab.Text = $Name
    $tab.BackColor = [Drawing.Color]::FromArgb(11,11,16)
    $flow = New-Object System.Windows.Forms.FlowLayoutPanel
    $flow.Dock = [System.Windows.Forms.DockStyle]::Fill
    $flow.AutoScroll = $true
    $flow.BackColor = $tab.BackColor
    foreach ($action in $Actions) {
        Add-ActionCard -Panel $flow -Action $action
    }
    $tab.Controls.Add($flow)
    $Tabs.TabPages.Add($tab) | Out-Null
}

if ($NoGui) {
    Get-NokaraActions | Out-Null
    return
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$form = New-Object System.Windows.Forms.Form
$form.Text = "Nokara System Manager - PowerShell Tool"
$form.Width = 1180
$form.Height = 760
$form.MinimumSize = New-Object Drawing.Size(980, 620)
$form.StartPosition = "CenterScreen"
$form.BackColor = [Drawing.Color]::FromArgb(11,11,16)
$form.ForeColor = [Drawing.Color]::FromArgb(242,242,245)

$top = New-Object System.Windows.Forms.Panel
$top.Dock = [System.Windows.Forms.DockStyle]::Top
$top.Height = 74
$top.BackColor = [Drawing.Color]::FromArgb(21,22,29)
$form.Controls.Add($top)

$title = New-DarkLabel -Text "Nokara PowerShell Tool" -Size 15 -Bold
$title.Left = 16
$title.Top = 12
$top.Controls.Add($title)

$admin = New-DarkLabel -Text ("Administrador: {0}" -f $(if (Test-NokaraAdmin) { "Si" } else { "No" })) -Size 9
$admin.Left = 18
$admin.Top = 43
$admin.ForeColor = if (Test-NokaraAdmin) { [Drawing.Color]::FromArgb(74,222,128) } else { [Drawing.Color]::FromArgb(245,158,11) }
$top.Controls.Add($admin)

$btnRun = New-Object System.Windows.Forms.Button
$btnRun.Text = "Ejecutar seleccion"
$btnRun.Width = 145
$btnRun.Height = 34
$btnRun.Left = 460
$btnRun.Top = 20
$top.Controls.Add($btnRun)

$btnUndo = New-Object System.Windows.Forms.Button
$btnUndo.Text = "Undo seleccion"
$btnUndo.Width = 125
$btnUndo.Height = 34
$btnUndo.Left = 614
$btnUndo.Top = 20
$top.Controls.Add($btnUndo)

$btnReport = New-Object System.Windows.Forms.Button
$btnReport.Text = "Reporte"
$btnReport.Width = 92
$btnReport.Height = 34
$btnReport.Left = 748
$btnReport.Top = 20
$top.Controls.Add($btnReport)

$btnAdmin = New-Object System.Windows.Forms.Button
$btnAdmin.Text = "Reiniciar admin"
$btnAdmin.Width = 125
$btnAdmin.Height = 34
$btnAdmin.Left = 850
$btnAdmin.Top = 20
$btnAdmin.Enabled = -not (Test-NokaraAdmin)
$top.Controls.Add($btnAdmin)

$btnClear = New-Object System.Windows.Forms.Button
$btnClear.Text = "Limpiar checks"
$btnClear.Width = 115
$btnClear.Height = 34
$btnClear.Left = 984
$btnClear.Top = 20
$top.Controls.Add($btnClear)

$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock = [System.Windows.Forms.DockStyle]::Fill
$tabs.BackColor = [Drawing.Color]::FromArgb(11,11,16)
$form.Controls.Add($tabs)

$actions = Get-NokaraActions
$overviewTab = New-Object System.Windows.Forms.TabPage
$overviewTab.Text = "Overview"
$overviewTab.BackColor = [Drawing.Color]::FromArgb(11,11,16)
$script:OverviewBox = New-Object System.Windows.Forms.TextBox
$script:OverviewBox.Multiline = $true
$script:OverviewBox.ReadOnly = $true
$script:OverviewBox.ScrollBars = "Vertical"
$script:OverviewBox.Dock = "Fill"
$script:OverviewBox.BackColor = [Drawing.Color]::FromArgb(27,28,35)
$script:OverviewBox.ForeColor = [Drawing.Color]::FromArgb(242,242,245)
$script:OverviewBox.Font = New-Object Drawing.Font("Consolas", 9)
$script:OverviewBox.Text = (Get-NokaraSnapshot | Format-List | Out-String)
$overviewTab.Controls.Add($script:OverviewBox)
$tabs.TabPages.Add($overviewTab) | Out-Null

Add-ActionTab -Tabs $tabs -Name "Tweaks" -Actions @($actions | Where-Object { $_.Module -in @("Mantenimiento","Config") })
Add-ActionTab -Tabs $tabs -Name "Gaming" -Actions @($actions | Where-Object { $_.Module -eq "Gaming" })
Add-ActionTab -Tabs $tabs -Name "Streaming" -Actions @($actions | Where-Object { $_.Module -eq "Streaming" })
Add-ActionTab -Tabs $tabs -Name "Network" -Actions @($actions | Where-Object { $_.Module -eq "Red" })
Add-ActionTab -Tabs $tabs -Name "Storage" -Actions @($actions | Where-Object { $_.Module -eq "Storage" })

$reportsTab = New-Object System.Windows.Forms.TabPage
$reportsTab.Text = "Reports"
$reportsTab.BackColor = [Drawing.Color]::FromArgb(11,11,16)
$script:LogBox = New-Object System.Windows.Forms.TextBox
$script:LogBox.Multiline = $true
$script:LogBox.ReadOnly = $true
$script:LogBox.ScrollBars = "Vertical"
$script:LogBox.Dock = "Fill"
$script:LogBox.BackColor = [Drawing.Color]::FromArgb(27,28,35)
$script:LogBox.ForeColor = [Drawing.Color]::FromArgb(242,242,245)
$script:LogBox.Font = New-Object Drawing.Font("Consolas", 9)
$reportsTab.Controls.Add($script:LogBox)
$tabs.TabPages.Add($reportsTab) | Out-Null

$aboutTab = New-Object System.Windows.Forms.TabPage
$aboutTab.Text = "About"
$aboutTab.BackColor = [Drawing.Color]::FromArgb(11,11,16)
$aboutText = New-DarkLabel -Text "Nokara System Manager v1.2.0`nEXE premium + PowerShell Lite + PowerShell Tool.`nNo magic FPS. No Defender/Firewall/Windows Update/BCDEDIT/HPET/MSI Mode.`n$script:ProjectUrl" -Size 10
$aboutText.Left = 18
$aboutText.Top = 18
$aboutText.AutoSize = $true
$aboutTab.Controls.Add($aboutText)
$tabs.TabPages.Add($aboutTab) | Out-Null

$btnRun.Add_Click({
    $selected = @(Get-CheckedActions)
    if ($selected.Count -eq 0) { Write-NokaraToolLog "WARN" "No hay acciones seleccionadas."; return }
    foreach ($action in $selected) { Invoke-NokaraAction -Action $action }
})

$btnUndo.Add_Click({
    $selected = @(Get-CheckedActions)
    if ($selected.Count -eq 0) { Write-NokaraToolLog "WARN" "No hay acciones seleccionadas para undo."; return }
    foreach ($action in $selected) { Invoke-NokaraAction -Action $action -Undo }
})

$btnReport.Add_Click({ Export-NokaraToolReport })
$btnAdmin.Add_Click({ Restart-NokaraToolAsAdmin })
$btnClear.Add_Click({ foreach ($item in $script:ActionControls) { $item.CheckBox.Checked = $false } })

Write-NokaraToolLog "INFO" "Nokara Tool iniciado."
[void][System.Windows.Forms.Application]::Run($form)
