@echo off
chcp 65001 >nul
echo ============================================
echo   RestoreSystem 安裝工具
echo ============================================
echo.

:: 檢查管理員權限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [錯誤] 請用「以系統管理員身分執行」重新開啟此檔案。
    pause
    exit /b 1
)

set INSTALL_DIR=%ProgramFiles%\RestoreSystem
set SERVICE_NAME=RestoreService
set SRC_UI=..\RestoreUI\bin\Release
set SRC_SVC=..\Restore.Service\bin\Release

echo [1/5] 停止舊服務（如果存在）...
sc stop %SERVICE_NAME% >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete %SERVICE_NAME% >nul 2>&1
timeout /t 1 /nobreak >nul

echo [2/5] 建立安裝目錄: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [3/5] 複製檔案...
copy /Y "%SRC_UI%\RestoreUI.exe"         "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\RestoreUI.exe.config"  "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Core.dll"      "%INSTALL_DIR%\" >nul
copy /Y "%SRC_UI%\Restore.Engine.dll"    "%INSTALL_DIR%\" >nul
copy /Y "%SRC_SVC%\Restore.Service.exe"  "%INSTALL_DIR%\" >nul

echo [4/5] 註冊 Windows 服務（開機自動啟動，登入前執行）...
sc create %SERVICE_NAME% binPath= "\"%INSTALL_DIR%\Restore.Service.exe\"" start= auto DisplayName= "Restore System Service"
sc description %SERVICE_NAME% "開機自動還原系統服務（登入前執行）"
sc failure %SERVICE_NAME% reset= 86400 actions= restart/60000/restart/60000/restart/60000

echo [5/5] 啟動服務...
sc start %SERVICE_NAME%

echo.
echo ============================================
echo   安裝完成！
echo   程式位置: %INSTALL_DIR%
echo   控制台:   %INSTALL_DIR%\RestoreUI.exe
echo ============================================
echo.

:: 建立桌面捷徑
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $sc = $ws.CreateShortcut([Environment]::GetFolderPath('CommonDesktopDirectory') + '\RestoreSystem 控制台.lnk'); $sc.TargetPath = '%INSTALL_DIR%\RestoreUI.exe'; $sc.Save()"
echo 已建立桌面捷徑。

pause
