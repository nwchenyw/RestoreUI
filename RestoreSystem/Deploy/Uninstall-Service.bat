@echo off
chcp 65001 >nul
echo ============================================
echo RestoreSystem Service 卸載程式
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

echo 正在停止服務...
sc stop RestoreSystemService
timeout /t 3 /nobreak >nul

echo.
echo 正在移除服務...
sc delete RestoreSystemService

echo.
echo ============================================
echo 卸載完成！
echo ============================================
pause
