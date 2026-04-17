@echo off
chcp 65001 >nul
echo ============================================
echo RestoreSystem Service 安裝程式
echo ============================================
echo.

REM 檢查是否以管理員身分執行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 錯誤：請以系統管理員身分執行此批次檔！
    echo 右鍵點擊檔案 -^> 以系統管理員身分執行
    pause
    exit /b 1
)

echo 正在停止並移除舊服務...
sc stop RestoreSystemService >nul 2>&1
sc delete RestoreSystemService >nul 2>&1
timeout /t 2 /nobreak >nul

echo.
echo 正在編譯專案...
cd /d "%~dp0"
dotnet build RestoreSystem.Service\RestoreSystem.Service.csproj -c Release
if %errorLevel% neq 0 (
    echo 編譯失敗！
    pause
    exit /b 1
)

echo.
echo 正在建立服務...
set SERVICE_PATH=%~dp0RestoreSystem.Service\bin\Release\net8.0\RestoreSystem.Service.exe

if not exist "%SERVICE_PATH%" (
    echo 錯誤：找不到服務執行檔！
    echo 路徑：%SERVICE_PATH%
    pause
    exit /b 1
)

sc create RestoreSystemService binPath= "%SERVICE_PATH%" start= auto
if %errorLevel% neq 0 (
    echo 建立服務失敗！
    pause
    exit /b 1
)

echo.
echo 正在啟動服務...
sc start RestoreSystemService
if %errorLevel% neq 0 (
    echo 啟動服務失敗！可能需要檢查日誌。
)

echo.
echo 服務狀態：
sc query RestoreSystemService

echo.
echo ============================================
echo 安裝完成！
echo ============================================
pause
