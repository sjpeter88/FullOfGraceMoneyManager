@echo off
setlocal EnableDelayedExpansion
title Full of Grace Money Manager - Windows 1-Click Installer (v1.0.0)
color 0B

echo ============================================================================
echo   FULL OF GRACE MONEY MANAGER (v1.0.0) - WINDOWS DESKTOP INSTALLER
echo   Architecture: .NET 10.0 WPF + WebView2 + SQLite Local Persistence
echo ============================================================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\FullofGraceMoneyManager"
set "PROJECT_PATH=%~dp0FullofGraceMoneyManager.Desktop\FullofGraceMoneyManager.Desktop.csproj"
set "HAS_DOTNET_10=0"

echo [1/4] Preparing installation directory:
echo       "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Check if .NET 10 SDK is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    dotnet --list-sdks | findstr /B "10\." >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        set "HAS_DOTNET_10=1"
    )
)

if "%HAS_DOTNET_10%"=="0" (
    echo.
    echo [*] .NET 10 SDK not detected locally. Attempting automatic install via winget...
    where winget >nul 2>&1
    if !ERRORLEVEL! EQU 0 (
        winget install --id Microsoft.DotNet.SDK.10 --exact --accept-source-agreements --accept-package-agreements
        if !ERRORLEVEL! EQU 0 (
            set "HAS_DOTNET_10=1"
        )
    )
)

echo.
if "%HAS_DOTNET_10%"=="1" (
    echo [2/4] Building and publishing native .NET 10 WPF executable ^(win-x64^)...
    dotnet publish "%PROJECT_PATH%" -c Release -r win-x64 --self-contained false -o "%INSTALL_DIR%"
    if !ERRORLEVEL! EQU 0 (
        set "TARGET_LAUNCHER=%INSTALL_DIR%\FullofGraceMoneyManager.Desktop.exe"
        set "TARGET_ICON=%SystemRoot%\System32\shell32.dll,277"
        goto :CreateShortcuts
    )
)

echo [2/4] Installing WebView2 Desktop App runtime files to "%INSTALL_DIR%"...
xcopy /E /I /Y "%~dp0FullofGraceMoneyManager.Desktop\wwwroot" "%INSTALL_DIR%\wwwroot" >nul
if exist "%~dp0FullofGraceMoneyManager.Desktop\Data\fullofgrace.db" (
    if not exist "%LOCALAPPDATA%\FullofGraceMoneyManager" mkdir "%LOCALAPPDATA%\FullofGraceMoneyManager"
    copy /Y "%~dp0FullofGraceMoneyManager.Desktop\Data\fullofgrace.db" "%LOCALAPPDATA%\FullofGraceMoneyManager\fullofgrace.db" >nul
)

:: Create native WebView2 App Launcher inside INSTALL_DIR
set "TARGET_LAUNCHER=%INSTALL_DIR%\FullofGraceMoneyManager-Launcher.vbs"
(
    echo Set WshShell = CreateObject^("WScript.Shell"^)
    echo AppUrl = "file:///" ^& Replace^("%INSTALL_DIR%\wwwroot\index.html", "\", "/"^)
    echo On Error Resume Next
    echo WshShell.Run "msedge.exe --app=""" ^& AppUrl ^& """ --window-size=1480,920", 1, False
    echo If Err.Number ^<^> 0 Then WshShell.Run """" ^& "%INSTALL_DIR%\wwwroot\index.html" ^& """", 1, False
) > "%TARGET_LAUNCHER%"

:CreateShortcuts
echo.
echo [3/4] Creating Desktop and Start Menu shortcuts...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$WshShell = New-Object -ComObject WScript.Shell; " ^
    "$DesktopPath = [Environment]::GetFolderPath('Desktop'); " ^
    "$StartMenuPath = [Environment]::GetFolderPath('Programs'); " ^
    "foreach ($LinkFile in @(\"$DesktopPath\Full of Grace Money Manager.lnk\", \"$StartMenuPath\Full of Grace Money Manager.lnk\")) { " ^
    "  $Shortcut = $WshShell.CreateShortcut($LinkFile); " ^
    "  $Shortcut.TargetPath = '%TARGET_LAUNCHER%'; " ^
    "  $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; " ^
    "  $Shortcut.Description = 'Full of Grace Money Manager - Personal Finance & Stewardship Desktop Suite'; " ^
    "  $Shortcut.Save(); " ^
    "}"

echo.
echo [4/4] Installation complete!
echo       Install Location: "%INSTALL_DIR%"
echo       Desktop Shortcut: "%USERPROFILE%\Desktop\Full of Grace Money Manager.lnk"
echo       Start Menu:       "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Full of Grace Money Manager.lnk"
echo.
echo Launching Full of Grace Money Manager...
start "" "%TARGET_LAUNCHER%"
exit /b 0
