@echo off
chcp 65001 >nul
echo ============================================
echo RestoreSystem 部署包建立工具
echo ============================================
echo.

cd /d "%~dp0"

REM 設定變數
set OUTPUT_DIR=%~dp0Deploy
set SERVICE_DIR=%OUTPUT_DIR%\Service
set UI_DIR=%OUTPUT_DIR%\UI

echo 清理舊的部署包...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"
mkdir "%SERVICE_DIR%"
mkdir "%UI_DIR%"

echo.
echo [1/4] 編譯 Service (Release)...
dotnet publish RestoreSystem.Service\RestoreSystem.Service.csproj -c Release -o "%SERVICE_DIR%" --self-contained false
if %errorLevel% neq 0 (
    echo 編譯 Service 失敗！
    pause
    exit /b 1
)

echo.
echo [2/4] 編譯 UI (Release)...
dotnet publish RestoreSystem.UI\RestoreSystem.UI.csproj -c Release -o "%UI_DIR%" --self-contained false
if %errorLevel% neq 0 (
    echo 編譯 UI 失敗！
    pause
    exit /b 1
)

echo.
echo [3/4] 複製安裝腳本...
copy /y "%~dp0Check-DotNetRuntime.bat" "%OUTPUT_DIR%\"
copy /y "%~dp0RestoreSystem.UI\Create-Shortcut.bat" "%OUTPUT_DIR%\"
copy /y "%~dp0RestoreSystem.Service\Install-Service.bat" "%OUTPUT_DIR%\"
copy /y "%~dp0RestoreSystem.Service\Uninstall-Service.bat" "%OUTPUT_DIR%\"
copy /y "%~dp0RestoreSystem.Service\Run-Debug.bat" "%OUTPUT_DIR%\"

echo.
echo [4/4] 建立安裝說明...
copy /y "%~dp0Install-VM.bat" "%OUTPUT_DIR%\"
(
echo RestoreSystem 部署包
echo ==================
echo.
echo 檔案結構：
echo   Service\         - 後端服務檔案
echo   UI\              - 使用者介面檔案
echo   Install-VM.bat   - VM 環境安裝腳本
echo   Install-Service.bat - 服務安裝腳本
echo.
echo 安裝步驟：
echo   1. 將整個 Deploy 資料夾複製到 VM
echo   2. 以系統管理員身分執行 Install-VM.bat
echo   3. 完成！
echo.
echo 注意：
echo   - 需要 .NET 8 Runtime
echo   - 需要系統管理員權限
echo.
) > "%OUTPUT_DIR%\README.txt"

echo OK 部署包準備完成

echo.
echo ============================================
echo 部署包建立完成！
echo ============================================
echo.
echo 輸出位置：%OUTPUT_DIR%
echo.
echo 下一步：
echo   1. 將 Deploy 資料夾複製到 VM
echo   2. 在 VM 上以系統管理員身分執行 Install-VM.bat
echo.
pause
