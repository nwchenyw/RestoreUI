; ============================================================
; RestoreSystem 安裝腳本 (Inno Setup)
; 
; 編譯方式：
;   1. 安裝 Inno Setup: https://jrsoftware.org/isinfo.php
;   2. 開啟此 .iss 檔案
;   3. 按 Ctrl+F9 編譯
;   4. 輸出的安裝 EXE 在 Installer\Output\ 資料夾
; ============================================================

#define MyAppName "RestoreSystem"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RestoreSystem"
#define MyAppExeName "RestoreUI.exe"
#define ServiceExeName "Restore.Service.exe"
#define ServiceName "RestoreService"

[Setup]
AppId={{B1C2D3E4-F5A6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=RestoreSystem_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=
WizardStyle=modern

[Languages]
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\\ChineseTraditional.isl"

[Dirs]
Name: "C:\\RestoreSystem"; Permissions: administrators-full system-full
Name: "C:\\RestoreSystem\\Logs"; Permissions: administrators-full system-full

[Files]
; UI 主程式
Source: "..\RestoreUI\bin\Release\RestoreUI.exe";         DestDir: "{app}"; Flags: ignoreversion
Source: "..\RestoreUI\bin\Release\RestoreUI.exe.config";  DestDir: "{app}"; Flags: ignoreversion
Source: "..\RestoreUI\bin\Release\Restore.Core.dll";      DestDir: "{app}"; Flags: ignoreversion
Source: "..\RestoreUI\bin\Release\Restore.Engine.dll";    DestDir: "{app}"; Flags: ignoreversion

; Windows Service
Source: "..\Restore.Service\bin\Release\Restore.Service.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Restore.Service\bin\Release\Restore.Core.dll";    DestDir: "{app}"; Flags: ignoreversion
Source: "..\Restore.Service\bin\Release\Restore.Engine.dll";  DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\RestoreSystem 控制台"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\解除安裝 RestoreSystem"; Filename: "{uninstallexe}"
Name: "{commondesktop}\RestoreSystem 控制台"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "其他選項:"

[Run]
; 安裝完成後註冊並啟動服務
Filename: "cmd.exe"; Parameters: "/c if not exist C:\\RestoreSystem mkdir C:\\RestoreSystem"; Flags: runhidden waituntilterminated
Filename: "cmd.exe"; Parameters: "/c icacls C:\\RestoreSystem /grant SYSTEM:(OI)(CI)F Administrators:(OI)(CI)F /T /C"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "create {#ServiceName} binPath= ""{app}\{#ServiceExeName}"" start= auto DisplayName= ""Restore System Service"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "description {#ServiceName} ""開機自動還原系統服務（登入前執行）"""; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "failure {#ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start {#ServiceName}"; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "開啟 RestoreSystem 控制台"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
; 解除安裝時停止並移除服務
Filename: "sc.exe"; Parameters: "stop {#ServiceName}";   Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#ServiceName}"; Flags: runhidden waituntilterminated

[Code]
// 安裝前如果服務已存在，先停止再移除
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    Exec('sc.exe', 'stop {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
    Exec('sc.exe', 'delete {#ServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);
  end;
end;
