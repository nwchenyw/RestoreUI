@echo off
setlocal
chcp 437 >nul

echo ============================================
echo   RestoreSystem Uninstaller
echo ============================================

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Please run as Administrator.
    pause
    exit /b 1
)

set "INSTALL_DIR=%ProgramFiles%\RestoreSystem"
set "SERVICE_NAME=RestoreService"

echo [1/4] Stop service...
sc stop %SERVICE_NAME% >nul 2>&1
timeout /t 2 /nobreak >nul

echo [2/4] Delete service...
sc delete %SERVICE_NAME% >nul 2>&1

echo [3/4] Remove files...
if exist "%INSTALL_DIR%" rmdir /S /Q "%INSTALL_DIR%"

echo [4/4] Keep C:\RestoreSystem data for safety.
echo Done.
pause
endlocal

