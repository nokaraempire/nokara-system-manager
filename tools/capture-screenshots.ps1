param(
    [string]$OutputDir = (Join-Path (Resolve-Path ".").Path "docs\screenshots"),
    [string]$WindowTitle = "Nokara",
    [string]$Name = "manual-capture"
)

$ErrorActionPreference = "Stop"

# Helper for release screenshots. Open the target Nokara window, navigate to a
# safe/redacted view, then run this script to capture the foreground app window.
# Review every image before committing it.
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$window = Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -like "*$WindowTitle*" } |
    Select-Object -First 1

if (-not $window) {
    throw "No window with title containing '$WindowTitle' was found. Open Nokara first."
}

$handle = $window.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) {
    throw "The Nokara process does not expose a main window handle yet."
}

[Win32Capture]::SetForegroundWindow($handle) | Out-Null
Start-Sleep -Milliseconds 350

$rect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($handle, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
if ($width -le 0 -or $height -le 0) {
    throw "Invalid window bounds."
}

$bitmap = New-Object Drawing.Bitmap($width, $height)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
$path = Join-Path $OutputDir ("{0}.png" -f $Name)
$bitmap.Save($path, [Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "Screenshot saved: $path"
