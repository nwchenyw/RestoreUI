param(
    [string]$PublishRoot = "C:\Program Files\RestoreSystem Pro",
    [string]$ServiceExe = "C:\Program Files\RestoreSystem Pro\RestoreSystem.Service.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "[1/6] 建立 C:\RestoreSystem 資料夾"
New-Item -ItemType Directory -Path "C:\RestoreSystem" -Force | Out-Null
New-Item -ItemType Directory -Path "C:\RestoreSystem\Snapshots" -Force | Out-Null

Write-Host "[2/6] 設定權限"
icacls "C:\RestoreSystem" /grant "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" /T /C | Out-Null

Write-Host "[3/6] 建立 base.vhdx（若缺少）"
if (-not (Test-Path "C:\RestoreSystem\base.vhdx")) {
    powershell -NoProfile -ExecutionPolicy Bypass -Command "New-VHD -Path 'C:\RestoreSystem\base.vhdx' -SizeBytes 64GB -Dynamic" | Out-Null
}

Write-Host "[4/6] 註冊服務"
sc.exe stop RestoreSystemService | Out-Null
sc.exe delete RestoreSystemService | Out-Null
sc.exe create RestoreSystemService binPath= "`"$ServiceExe`"" start= auto obj= "LocalSystem" DisplayName= "RestoreSystem Pro Service" | Out-Null
sc.exe failure RestoreSystemService reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "[5/6] 建立 Restore Mode 開機項目"
$copyOutput = cmd /c 'bcdedit /copy {current} /d "Restore Mode"'
$guid = ([regex]"\{[0-9a-fA-F-]+\}").Match($copyOutput).Value
$normalOutput = cmd /c 'bcdedit /copy {current} /d "Normal Mode"'
$normalGuid = ([regex]"\{[0-9a-fA-F-]+\}").Match($normalOutput).Value
if ($guid) {
    cmd /c "bcdedit /set $guid device vhd=[C:]\RestoreSystem\diff.vhdx" | Out-Null
    cmd /c "bcdedit /set $guid osdevice vhd=[C:]\RestoreSystem\diff.vhdx" | Out-Null
    cmd /c "bcdedit /set $guid detecthal on" | Out-Null
    cmd /c "bcdedit /timeout 5" | Out-Null
}

Write-Host "[6/6] 啟動服務"
sc.exe start RestoreSystemService | Out-Null

Write-Host "安裝完成。"
