@echo off
chcp 65001 >nul
echo ============================================
echo RestoreSystem Service 測試模式
echo (Console Mode - 不安裝為 Windows 服務)
echo ============================================
echo.

cd /d "%~dp0"

echo 正在編譯專案...
dotnet build RestoreSystem.Service\RestoreSystem.Service.csproj -c Debug
if %errorLevel% neq 0 (
    echo 編譯失敗！
    pause
    exit /b 1
)

echo.
echo 正在啟動服務（主控台模式）...
echo 按 Ctrl+C 可停止服務
echo.

dotnet run --project RestoreSystem.Service\RestoreSystem.Service.csproj

pause
