param(
    [string]$ServiceExe = "C:\Program Files\TopCPR\TopCPR.Service.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "Create folders..."
New-Item -ItemType Directory -Path "C:\TopCPR" -Force | Out-Null
New-Item -ItemType Directory -Path "C:\TopCPR\logs" -Force | Out-Null
New-Item -ItemType Directory -Path "C:\TopCPR\snapshots" -Force | Out-Null

Write-Host "Create base VHD if missing..."
if (-not (Test-Path "C:\TopCPR\base.vhdx")) {
    powershell -NoProfile -ExecutionPolicy Bypass -Command "New-VHD -Path 'C:\TopCPR\base.vhdx' -SizeBytes 64GB -Dynamic" | Out-Null
}

Write-Host "Register service..."
sc.exe stop TopCPRService | Out-Null
sc.exe delete TopCPRService | Out-Null
sc.exe create TopCPRService binPath= "`"$ServiceExe`"" start= auto obj= "LocalSystem" DisplayName= "Top CPR Service" | Out-Null
sc.exe failure TopCPRService reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
sc.exe start TopCPRService | Out-Null

Write-Host "Install complete."
