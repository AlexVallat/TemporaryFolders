#define AppName "Temporary Folders"
#define AppPublisher "Alex Vallat"
#define AppURL "https://github.com/AlexVallat/TemporaryFolders"
#define AppExeName "TempFolder.exe"
#define AppVersion GetVersionNumbersString("..\bin\Publish\" + AppExeName)
#if AppVersion == ""
#error GetVersionNumbersString("..\bin\Publish\" + AppExeName + ")") failed
#endif

#define public Dependency_Path_NetCoreCheck "DependencyInstaller\dependencies\"
#include "DependencyInstaller\CodeDependencies.iss"

[CustomMessages]
ShortcutName=New Temporary Folder
CreateSendToIcon=Create a &Send To shortcut
SusbtDrive=&Map T: drive to most recent temporary folder (restart required)
DriveName=Temporary Folder

[Messages]
UninstalledAndNeedsRestart=Uninstall completed. To remove the mapped T: drive, your computer must be restarted.%n%nWould you like to restart now?

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{54AF08CC-57FB-4EBB-AD77-5B1CFA026C48}
AppName={#AppName}
AppVersion={#AppVersion}
;AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppSupportURL={#AppURL}
UninstallDisplayIcon={app}\{#AppExeName}
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=TemporaryFolders-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
VersionInfoCompany={#AppPublisher}
VersionInfoProductName={#AppName} Setup
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoCopyright={#AppURL}

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet80Desktop;

  Result := True;
end;

function AllowSubstDrive: Boolean;
begin
  Result := IsAdmin and not DirExists('T:\');
end;

var
  uninstallRebootRequired: Boolean;

function InitializeUninstall: Boolean;
begin
  uninstallRebootRequired := RegValueExists(HKLM, 'System\CurrentControlSet\Control\Session Manager\DOS Devices', 'T:')
  Result := True;
end;

function UninstallNeedRestart(): Boolean;
begin
  Result := uninstallRebootRequired and not RegValueExists(HKLM, 'System\CurrentControlSet\Control\Session Manager\DOS Devices', 'T:');
end;

[UninstallRun]
Filename: "taskkill"; Parameters: "/im ""{#AppExeName}"" /f"; Flags: runhidden; RunOnceId: "KillAppProcess"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "sendtoicon"; Description: "{cm:CreateSendToIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "susbtdrive"; Description: "{cm:SusbtDrive}"; Flags: unchecked restart; Check: AllowSubstDrive

[Files]
Source: "..\bin\Publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\T"; Flags: uninsdeletekey; Tasks: susbtdrive
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\T\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExeName},0"; Tasks: susbtdrive
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\T\DefaultLabel"; ValueType: string; ValueData: "{cm:DriveName}"; Tasks: susbtdrive

Root: HKLM; Subkey: "System\CurrentControlSet\Control\Session Manager\DOS Devices"; ValueName: "T:"; ValueType: string; ValueData: "\??\{commonappdata}\Most Recent Temporary Folder"; Flags: uninsdeletevalue; Tasks: susbtdrive

[UninstallDelete]
Type: dirifempty; Name: "{commonappdata}\Most Recent Temporary Folder"

[Icons]
Name: "{autoprograms}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{usersendto}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: sendtoicon

