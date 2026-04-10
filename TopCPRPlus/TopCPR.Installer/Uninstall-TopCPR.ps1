$ErrorActionPreference = "Continue"

sc.exe stop TopCPRService | Out-Null
sc.exe delete TopCPRService | Out-Null
cmd /c "bcdedit /default {current}" | Out-Null

Write-Host "Uninstall complete."
