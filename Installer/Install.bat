@echo off
setlocal
chcp 437 >nul

set "SCRIPT_DIR=%~dp0"

echo ============================================
echo   RestoreSystem Installer
echo ============================================

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Please run as Administrator.
    pause
    exit /b 1
)

set "INSTALL_DIR=%ProgramFiles%\RestoreSystem"
set "SERVICE_NAME=RestoreService"
set "SRC_UI=%SCRIPT_DIR%..\RestoreUI\bin\Release"
set "SRC_SVC=%SCRIPT_DIR%..\Restore.Service\bin\Release"
set "PKG_DIR=%SCRIPT_DIR%Package"
set "UI_CSPROJ=%SCRIPT_DIR%..\RestoreUI\RestoreUI.csproj"
set "SVC_CSPROJ=%SCRIPT_DIR%..\Restore.Service\Restore.Service.csproj"

if exist "%PKG_DIR%\RestoreUI.exe" if exist "%PKG_DIR%\Restore.Service.exe" goto :use_package

if not exist "%SRC_UI%\RestoreUI.exe" goto :build_release
if not exist "%SRC_SVC%\Restore.Service.exe" goto :build_release
goto :install_begin

:use_package
echo [INFO] Using prebuilt package files from: %PKG_DIR%
set "SRC_UI=%PKG_DIR%"
set "SRC_SVC=%PKG_DIR%"
goto :install_begin

:build_release
echo [INFO] Release output not found. Building solution in Release mode...
set "MSBUILD_EXE="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not "%%i"=="" set "MSBUILD_EXE=%%i"
    )
)

if not "%MSBUILD_EXE%"=="" goto :msbuild_ready

where msbuild >nul 2>&1
if %errorlevel% equ 0 (
    set "MSBUILD_EXE=msbuild"
    goto :msbuild_ready
)

if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if not "%MSBUILD_EXE%"=="" goto :msbuild_ready

if "%MSBUILD_EXE%"=="" (
    echo [ERROR] msbuild not found.
    echo [HINT] This project targets .NET Framework 4.8.
    echo [HINT] Use prebuilt files in Installer\Package, or install VS Build Tools + .NET Framework 4.8 Developer Pack.
    pause
    exit /b 1
)

:msbuild_ready
if not "%MSBUILD_EXE%"=="" echo [INFO] Using MSBuild: %MSBUILD_EXE%

pushd "%SCRIPT_DIR%.."
if not "%MSBUILD_EXE%"=="" (
    if not exist "%UI_CSPROJ%" (
        popd
        echo [ERROR] Missing project: %UI_CSPROJ%
        pause
        exit /b 1
    )
    if not exist "%SVC_CSPROJ%" (
        popd
        echo [ERROR] Missing project: %SVC_CSPROJ%
        pause
        exit /b 1
    )

    "%MSBUILD_EXE%" "%UI_CSPROJ%" /t:Rebuild /p:Configuration=Release /verbosity:minimal
    if errorlevel 1 (
        popd
        echo [ERROR] Release build failed (UI).
        pause
        exit /b 1
    )

    "%MSBUILD_EXE%" "%SVC_CSPROJ%" /t:Rebuild /p:Configuration=Release /verbosity:minimal
)
if errorlevel 1 (
    popd
    echo [ERROR] Release build failed.
    pause
    exit /b 1
)
popd

if not exist "%SRC_UI%\RestoreUI.exe" (
    echo [ERROR] Missing file after build: %SRC_UI%\RestoreUI.exe
    pause
    exit /b 1
)
if not exist "%SRC_SVC%\Restore.Service.exe" (
    echo [ERROR] Missing file after build: %SRC_SVC%\Restore.Service.exe
    pause
    exit /b 1
)

:install_begin

echo [1/6] Stop and remove old service...
sc stop %SERVICE_NAME% >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete %SERVICE_NAME% >nul 2>&1
timeout /t 1 /nobreak >nul

echo [2/6] Create folders...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "C:\RestoreSystem" mkdir "C:\RestoreSystem"
if not exist "C:\RestoreSystem\Logs" mkdir "C:\RestoreSystem\Logs"

echo [3/6] Copy files...
copy /Y "%SRC_UI%\RestoreUI.exe" "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\RestoreUI.exe.config" "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Core.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Engine.dll" "%INSTALL_DIR%\" >nul
copy /Y "%SRC_SVC%\Restore.Service.exe" "%INSTALL_DIR%\" >nul

if not exist "%INSTALL_DIR%\Restore.Service.exe" (
    echo [ERROR] Service executable not found after copy:
    echo         %INSTALL_DIR%\Restore.Service.exe
    echo [HINT] Please build Release first, then run this installer from the Installer folder.
    pause
    exit /b 1
)

echo [4/6] Set folder permissions...
icacls "C:\RestoreSystem" /grant SYSTEM:(OI)(CI)F Administrators:(OI)(CI)F /T /C >nul

echo [5/6] Register service...
sc create %SERVICE_NAME% binPath= "\"%INSTALL_DIR%\Restore.Service.exe\"" start= auto DisplayName= "Restore System Service"
sc description %SERVICE_NAME% "RestoreSystem auto restore service"
sc failure %SERVICE_NAME% reset= 86400 actions= restart/60000/restart/60000/restart/60000

sc qc %SERVICE_NAME%

echo [6/6] Start service...
sc start %SERVICE_NAME%

echo Done.
echo UI: %INSTALL_DIR%\RestoreUI.exe
pause
endlocal

