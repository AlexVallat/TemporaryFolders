#define AppName "Temporary Folders"
#define AppPublisher "Alex Vallat"
#define AppURL "https://github.com/AlexVallat/TemporaryFolders"
#define AppExeName "TempFolder.exe"

#ifndef AppVersion
#define AppVersion "9.9.9.9"
#endif

#define public Dependency_NoExampleSetup
#define UseNetCoreCheck
#include "DependencyInstaller\CodeDependencies.iss"

[CustomMessages]
ShortcutName=New Temporary Folder
CreateSendToIcon=Create a &Send To shortcut

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
  Dependency_AddDotNet60Desktop;

  Result := True;
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "sendtoicon"; Description: "{cm:CreateSendToIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "DependencyInstaller\src\netcorecheck.exe"; Flags: dontcopy noencryption
Source: "DependencyInstaller\src\netcorecheck_x64.exe"; Flags: dontcopy noencryption


Source: "..\bin\Publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{usersendto}\{cm:ShortcutName}"; Filename: "{app}\{#AppExeName}"; Tasks: sendtoicon

