@echo off
chcp 65001 >nul
echo ============================================
echo RestoreSystem VM 環境安裝程式
echo ============================================
echo.

REM 檢查管理員權限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 錯誤：請以系統管理員身分執行！
    pause
    exit /b 1
)

echo [1/5] 檢查 .NET 8 Runtime...
dotnet --version >nul 2>&1
if %errorLevel% neq 0 (
    echo 錯誤：未安裝 .NET 8 Runtime！
    echo 請先執行 Check-DotNetRuntime.bat 安裝
    pause
    exit /b 1
)
echo OK .NET Runtime 已安裝
echo.

echo [2/5] 建立目錄...
if not exist "C:\RestoreSystem" mkdir "C:\RestoreSystem"
if not exist "C:\RestoreSystem\Logs" mkdir "C:\RestoreSystem\Logs"
echo OK 目錄建立完成
echo.

echo [3/5] 複製檔案...
xcopy /s /y /q "%~dp0Service\*" "C:\RestoreSystem\Service\" >nul
xcopy /s /y /q "%~dp0UI\*" "C:\RestoreSystem\UI\" >nul
echo OK 檔案複製完成
echo.

echo [4/5] 建立預設設定檔...
if not exist "C:\RestoreSystem\config.json" (
    (
    echo {
    echo   "Enabled": false,
    echo   "PasswordHash": "",
    echo   "AuthTokenHash": "",
    echo   "ProtectDrive": "C",
    echo   "DataDrive": "D",
    echo   "BootEntryGuid": "",
    echo   "NormalBootEntryGuid": "",
    echo   "CurrentSnapshotId": "",
    echo   "AdminModeEnabled": false,
    echo   "SessionTimeoutMinutes": 10,
    echo   "BootTimeoutSeconds": 5,
    echo   "ForceVmSafeMode": true,
    echo   "AutoDetectVirtualMachine": true
    echo }
    ) > "C:\RestoreSystem\config.json"
    echo OK 已建立預設設定 - VM 安全模式已啟用
) else (
    echo OK 設定檔已存在，保持不變
)
echo.

echo [5/5] 安裝並啟動服務...
sc stop RestoreSystemService >nul 2>&1
sc delete RestoreSystemService >nul 2>&1
timeout /t 2 /nobreak >nul

sc create RestoreSystemService binPath= "C:\RestoreSystem\Service\RestoreSystem.Service.exe" start= auto
if %errorLevel% neq 0 (
    echo 建立服務失敗！
    pause
    exit /b 1
)
echo OK 服務已建立

sc start RestoreSystemService
if %errorLevel% neq 0 (
    echo 警告：啟動服務失敗！請檢查日誌
) else (
    echo OK 服務已啟動
)
echo.

echo ============================================
echo 安裝完成！
echo ============================================
echo.
echo 服務狀態：
sc query RestoreSystemService
echo.
echo UI 路徑：C:\RestoreSystem\UI\RestoreSystem.UI.exe
echo.
echo 建議：
echo   1. 執行 Create-Shortcut.bat 建立桌面捷徑
echo   2. 執行 UI 並設定密碼
echo   3. 在 Settings 檢查 VM 偵測結果
echo.
pause
