; Inno Setup script for Helpers.
; Per-user install, no admin rights. Built by CI on windows-latest, which ships Inno Setup;
; locally: iscc installer\Helpers.iss /DVersion=0.1.0 /DPublishDir=..\out\publish\win-x64

#ifndef Version
  #define Version "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\out\publish\win-x64"
#endif

[Setup]
AppId={{9E1D1F0E-6C1C-4E35-9B38-5B9C2B8E3F11}
AppName=Helpers
AppVersion={#Version}
AppVerName=Helpers {#Version}
AppPublisher=Helpers contributors
AppPublisherURL=https://github.com/drhumphrey/helpers
AppSupportURL=https://github.com/drhumphrey/helpers/issues
DefaultDirName={localappdata}\Programs\Helpers
DefaultGroupName=Helpers
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\out
OutputBaseFilename=Helpers-{#Version}-setup
SetupIconFile=..\src\Helpers.App\Assets\app.ico
UninstallDisplayIcon={app}\Helpers.App.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start Helpers when I sign in to Windows"; GroupDescription: "Start-up:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Helpers"; Filename: "{app}\Helpers.App.exe"
Name: "{group}\Uninstall Helpers"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Helpers"; ValueData: """{app}\Helpers.App.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\Helpers.App.exe"; Description: "Start Helpers now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/IM Helpers.App.exe /F"; Flags: runhidden; RunOnceId: "StopHelpers"

[Code]
// The voice model and settings live under the user's profile and are left alone on uninstall:
//   %LOCALAPPDATA%\Helpers\models   (about 350 MB, downloaded on first run)
//   %APPDATA%\Helpers\settings.json
