@echo off
setlocal
title BPF PICO main Export and Install

cd /d "%~dp0.."

for /f "delims=" %%B in ('git branch --show-current 2^>nul') do set "BPF_BRANCH=%%B"
if not "%BPF_BRANCH%"=="main" (
    echo [ERROR] This launcher must run from branch main. Current branch: %BPF_BRANCH%
    pause
    exit /b 1
)

echo Branch: %BPF_BRANCH%
echo Starting clean PICO export, Gradle build, ADB install, and app launch...
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass ^
  -File "%CD%\Tooling\build-and-install-pico.ps1" ^
  -CleanExport

set "EXIT_CODE=%ERRORLEVEL%"
echo.
if "%EXIT_CODE%"=="0" (
    echo [OK] Export, install, and launch completed.
) else (
    echo [ERROR] The export/install flow failed with exit code %EXIT_CODE%.
)
echo Close this window, or press any key to finish.
pause >nul
exit /b %EXIT_CODE%
