<#
.SYNOPSIS
    Builds and packages Full of Grace Money Manager for Windows (win-x64):
    1. Self-contained Windows release folder (dist\win-x64)
    2. Portable ZIP archive (dist\FullOfGraceMoneyManager-v1.0.0-win-x64-Portable.zip)
    3. Native Windows Setup .exe Installer via Inno Setup (dist\FullOfGraceMoneyManager-Setup-v1.0.0-win-x64.exe)
    4. Optional local user installation with Desktop & Start Menu shortcuts (-InstallNow)
#>
param(
    [switch]$SelfContained = $true,
    [switch]$InstallNow = $false
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $RepoRoot "FullofGraceMoneyManager.Desktop\FullofGraceMoneyManager.Desktop.csproj"
$DistRoot = Join-Path $RepoRoot "dist"
$PublishDir = Join-Path $DistRoot "win-x64"
$ZipPath = Join-Path $DistRoot "FullOfGraceMoneyManager-v1.0.0-win-x64-Portable.zip"
$IssFile = Join-Path $RepoRoot "installer\FullofGraceSetup.iss"

Write-Host "=======================================================================" -ForegroundColor Cyan
Write-Host "  Full of Grace Money Manager - Windows Packaging & Installer Studio   " -ForegroundColor Cyan
Write-Host "=======================================================================" -ForegroundColor Cyan

if (Test-Path $DistRoot) {
    Remove-Item $DistRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

$scFlag = if ($SelfContained) { "true" } else { "false" }
Write-Host "`n[1/3] Publishing .NET 10.0 WPF application (SelfContained=$scFlag, Runtime=win-x64)..." -ForegroundColor Yellow
dotnet publish $ProjectFile `
    -c Release `
    -r win-x64 `
    --self-contained $scFlag `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Write-Host "`n[2/3] Creating Portable ZIP bundle: $ZipPath" -ForegroundColor Yellow
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
Write-Host "      Created: $ZipPath" -ForegroundColor Green

# Check for Inno Setup Compiler (ISCC.exe) to build native Setup .exe
$IsccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$IsccExe = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($IsccExe -and (Test-Path $IssFile)) {
    Write-Host "`n[3/3] Compiling Windows Setup Wizard (.exe) with Inno Setup..." -ForegroundColor Yellow
    & $IsccExe $IssFile
    Write-Host "      Created: $DistRoot\FullOfGraceMoneyManager-Setup-v1.0.0-win-x64.exe" -ForegroundColor Green
} else {
    Write-Host "`n[3/3] Inno Setup 6 (ISCC.exe) not detected locally; skipped Setup.exe wrapper." -ForegroundColor DarkYellow
    Write-Host "      Tip: Install Inno Setup ('winget install JRSoftware.InnoSetup') and re-run to produce Setup.exe." -ForegroundColor Gray
}

if ($InstallNow) {
    $LocalInstallDir = Join-Path $env:LOCALAPPDATA "Programs\FullofGraceMoneyManager"
    Write-Host "`n[*] Installing to $LocalInstallDir and creating shortcuts..." -ForegroundColor Cyan
    if (-not (Test-Path $LocalInstallDir)) {
        New-Item -ItemType Directory -Path $LocalInstallDir -Force | Out-Null
    }
    Copy-Item -Path "$PublishDir\*" -Destination $LocalInstallDir -Recurse -Force

    $WshShell = New-Object -ComObject WScript.Shell
    $DesktopLink = Join-Path ([Environment]::GetFolderPath("Desktop")) "Full of Grace Money Manager.lnk"
    $StartMenuLink = Join-Path ([Environment]::GetFolderPath("Programs")) "Full of Grace Money Manager.lnk"

    foreach ($lnk in @($DesktopLink, $StartMenuLink)) {
        $s = $WshShell.CreateShortcut($lnk)
        $s.TargetPath = Join-Path $LocalInstallDir "FullofGraceMoneyManager.Desktop.exe"
        $s.WorkingDirectory = $LocalInstallDir
        $s.Description = "Full of Grace Money Manager"
        $s.Save()
    }

    Write-Host "Installed and shortcuts created! Launching application..." -ForegroundColor Green
    Start-Process (Join-Path $LocalInstallDir "FullofGraceMoneyManager.Desktop.exe")
}
