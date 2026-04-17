@echo off
chcp 65001 >nul
echo ============================================
echo .NET 8 Runtime 檢查與安裝
echo ============================================
echo.

echo 檢查 .NET 8 Runtime...
dotnet --list-runtimes | findstr "Microsoft.NETCore.App 8." >nul 2>&1

if %errorLevel% equ 0 (
    echo ✓ .NET 8 Runtime 已安裝
    echo.
    dotnet --list-runtimes | findstr "Microsoft.NETCore.App 8."
    echo.
    echo 您可以繼續執行 Install-VM.bat
    pause
    exit /b 0
)

echo.
echo ✗ 未偵測到 .NET 8 Runtime
echo.
echo 請選擇：
echo   1. 自動下載並安裝 .NET 8 Runtime
echo   2. 手動下載安裝
echo   3. 取消
echo.
choice /c 123 /n /m "請選擇 [1/2/3]: "

if %errorLevel% equ 1 goto AUTO_INSTALL
if %errorLevel% equ 2 goto MANUAL_INSTALL
if %errorLevel% equ 3 goto END

:AUTO_INSTALL
echo.
echo 正在下載 .NET 8 Runtime (Desktop)...
echo 下載位置：https://dotnet.microsoft.com/download/dotnet/8.0

REM 使用 PowerShell 下載
powershell -Command "& {Invoke-WebRequest -Uri 'https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x64-installer' -OutFile '%TEMP%\dotnet-runtime-8.0-win-x64.exe'}"

if exist "%TEMP%\dotnet-runtime-8.0-win-x64.exe" (
    echo.
    echo 開始安裝...
    start /wait "%TEMP%\dotnet-runtime-8.0-win-x64.exe" /install /quiet /norestart
    
    echo.
    echo 安裝完成！
    del "%TEMP%\dotnet-runtime-8.0-win-x64.exe"
) else (
    echo 下載失敗，請手動下載。
)
goto END

:MANUAL_INSTALL
echo.
echo 請前往以下網址下載 .NET 8 Desktop Runtime:
echo https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo 選擇：
echo   - Windows x64 Desktop Runtime
echo   - 或 Windows x64 Runtime (含 Desktop)
echo.
start https://dotnet.microsoft.com/download/dotnet/8.0
goto END

:END
pause
