@echo off
setlocal
chcp 437 >nul

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."
set "PKG_DIR=%SCRIPT_DIR%Package"
set "SRC_UI=%ROOT_DIR%\RestoreUI\bin\Release"
set "SRC_SVC=%ROOT_DIR%\Restore.Service\bin\Release"
set "UI_CSPROJ=%ROOT_DIR%\RestoreUI\RestoreUI.csproj"
set "SVC_CSPROJ=%ROOT_DIR%\Restore.Service\Restore.Service.csproj"

echo ============================================
echo   RestoreSystem Package Builder
echo ============================================

if not exist "%PKG_DIR%" mkdir "%PKG_DIR%"

echo Building Release...
set "MSBUILD_EXE="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not "%%i"=="" set "MSBUILD_EXE=%%i"
    )
)

if "%MSBUILD_EXE%"=="" where msbuild >nul 2>&1 && set "MSBUILD_EXE=msbuild"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if "%MSBUILD_EXE%"=="" if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if "%MSBUILD_EXE%"=="" (
    echo [ERROR] msbuild not found.
    echo [HINT] This project targets .NET Framework 4.8.
    echo [HINT] Install Visual Studio Build Tools + .NET Framework 4.8 Developer Pack.
    pause
    exit /b 1
)

echo [INFO] Using MSBuild: %MSBUILD_EXE%

pushd "%ROOT_DIR%"
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

"%MSBUILD_EXE%" "%UI_CSPROJ%" /t:Rebuild /p:Configuration=Release /verbosity:minimal || goto :build_fail_ui
"%MSBUILD_EXE%" "%SVC_CSPROJ%" /t:Rebuild /p:Configuration=Release /verbosity:minimal || goto :build_fail_service
popd
goto :copy_files

:build_fail_ui
popd
echo [ERROR] Release build failed (UI).
pause
exit /b 1

:build_fail_service
popd
echo [ERROR] Release build failed (Service).
pause
exit /b 1

:copy_files

echo Copying files to Package...
copy /Y "%SRC_UI%\RestoreUI.exe" "%PKG_DIR%\" >nul
copy /Y "%SRC_UI%\RestoreUI.exe.config" "%PKG_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Core.dll" "%PKG_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Engine.dll" "%PKG_DIR%\" >nul
copy /Y "%SRC_SVC%\Restore.Service.exe" "%PKG_DIR%\" >nul
copy /Y "%SRC_SVC%\Restore.Service.exe.config" "%PKG_DIR%\" >nul

echo Done.
echo Output: %PKG_DIR%
pause
endlocal
