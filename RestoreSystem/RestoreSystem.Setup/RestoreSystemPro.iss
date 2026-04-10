#define MyAppName "RestoreSystem Pro"
#define MyAppVersion "1.0.0"
#define ServiceExe "RestoreSystem.Service.exe"
#define UIExe "RestoreSystem.UI.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\RestoreSystem Pro
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=RestoreSystemPro_Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"

[Files]
Source: "..\Publish\Service\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "..\Publish\UI\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "Install-RestoreSystem.ps1"; DestDir: "{app}\Setup"; Flags: ignoreversion
Source: "Uninstall-RestoreSystem.ps1"; DestDir: "{app}\Setup"; Flags: ignoreversion

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File \"{app}\Setup\Install-RestoreSystem.ps1\" -ServiceExe \"{app}\{#ServiceExe}\""; Flags: runhidden waituntilterminated
Filename: "{app}\{#UIExe}"; Description: "啟動 RestoreSystem Pro"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File \"{app}\Setup\Uninstall-RestoreSystem.ps1\""; Flags: runhidden waituntilterminated
