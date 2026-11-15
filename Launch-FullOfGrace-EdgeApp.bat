@echo off
setlocal
title Full of Grace Money Manager - Instant Windows App Mode

set "APP_HTML=%~dp0FullofGraceMoneyManager.Desktop\wwwroot\index.html"

:: Check if compiled WPF .exe already exists in %LOCALAPPDATA%\Programs\FullofGraceMoneyManager
set "INSTALLED_EXE=%LOCALAPPDATA%\Programs\FullofGraceMoneyManager\FullofGraceMoneyManager.Desktop.exe"
if exist "%INSTALLED_EXE%" (
    echo Launching installed WPF desktop executable...
    start "" "%INSTALLED_EXE%"
    exit /b 0
)

:: Otherwise launch immediately in native Windows Edge WebView2 App Mode (--app)
echo Launching Full of Grace Money Manager in Windows Desktop App Window...
start "" msedge.exe --app="file:///%APP_HTML:\=/%" --window-size=1480,920
if %ERRORLEVEL% NEQ 0 (
    start "" "%APP_HTML%"
)
exit /b 0
