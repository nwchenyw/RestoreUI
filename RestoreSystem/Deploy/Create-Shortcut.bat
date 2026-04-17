@echo off
chcp 65001 >nul
echo 建立 RestoreSystem UI 桌面捷徑...

set TARGET="C:\RestoreSystem\UI\RestoreSystem.UI.exe"
set SHORTCUT="%USERPROFILE%\Desktop\RestoreSystem.lnk"

powershell -Command "$WS = New-Object -ComObject WScript.Shell; $SC = $WS.CreateShortcut('%SHORTCUT%'); $SC.TargetPath = '%TARGET%'; $SC.WorkingDirectory = 'C:\RestoreSystem\UI'; $SC.Description = 'RestoreSystem 還原系統控制台'; $SC.Save()"

if %errorLevel% equ 0 (
    echo ✓ 桌面捷徑已建立
) else (
    echo ✗ 建立捷徑失敗
)

pause
