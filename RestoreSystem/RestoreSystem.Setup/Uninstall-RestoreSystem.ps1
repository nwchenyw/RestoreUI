$ErrorActionPreference = "Continue"

Write-Host "停止並移除服務..."
sc.exe stop RestoreSystemService | Out-Null
sc.exe delete RestoreSystemService | Out-Null

Write-Host "還原預設開機項目..."
cmd /c "bcdedit /default {current}" | Out-Null

Write-Host "移除程式資料夾（保留 C:\RestoreSystem 以便復原）..."
# 依需求可保留資料，避免誤刪基底磁碟

Write-Host "解除安裝完成。"
