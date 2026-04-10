@echo off
chcp 65001 >nul
echo ============================================
echo   RestoreSystem 解除安裝工具
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

echo [1/4] 停止服務...
sc stop %SERVICE_NAME%
timeout /t 3 /nobreak >nul

echo [2/4] 移除服務...
sc delete %SERVICE_NAME%
timeout /t 1 /nobreak >nul

echo [3/4] 刪除程式檔案...
if exist "%INSTALL_DIR%" rmdir /S /Q "%INSTALL_DIR%"

echo [4/4] 刪除桌面捷徑...
del /F /Q "%PUBLIC%\Desktop\RestoreSystem 控制台.lnk" >nul 2>&1

echo.
echo ============================================
echo   解除安裝完成！
echo ============================================
pause
