; Inno Setup script for Unmuddle (the code's working title, and this file's name, is Helpers).
; Per-user install, no admin rights. Built by CI on windows-latest, which ships Inno Setup;
; locally: iscc installer\Helpers.iss /DVersion=0.2.0 /DPublishDir=..\out\publish\win-x64

#ifndef Version
  #define Version "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\out\publish\win-x64"
#endif

[Setup]
; The same AppId as when the app was called Helpers, so installing Unmuddle replaces that install
; and its entry in Settings > Apps instead of adding a second one.
AppId={{9E1D1F0E-6C1C-4E35-9B38-5B9C2B8E3F11}
AppName=Unmuddle
AppVersion={#Version}
AppVerName=Unmuddle {#Version}
AppPublisher=Unmuddle contributors
AppPublisherURL=https://github.com/drhumphrey/unmuddle
AppSupportURL=https://github.com/drhumphrey/unmuddle/issues
DefaultDirName={localappdata}\Programs\Unmuddle
; An install from the Helpers days lives in Programs\Helpers under a Helpers Start menu folder;
; don't reuse those. The start-up choice is still carried over.
UsePreviousAppDir=no
UsePreviousGroup=no
DefaultGroupName=Unmuddle
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\out
OutputBaseFilename=Unmuddle-{#Version}-setup
SetupIconFile=..\src\Helpers.App\Assets\app.ico
UninstallDisplayIcon={app}\Unmuddle.exe
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
Name: "startup"; Description: "Start Unmuddle when I sign in to Windows"; GroupDescription: "Start-up:"

[InstallDelete]
; The program files and Start menu folder from when the app was called Helpers. Settings and
; downloads are elsewhere, and the app moves them across itself on its first start.
Type: filesandordirs; Name: "{localappdata}\Programs\Helpers"
Type: filesandordirs; Name: "{userprograms}\Helpers"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Unmuddle"; Filename: "{app}\Unmuddle.exe"
Name: "{group}\Uninstall Unmuddle"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Unmuddle"; ValueData: """{app}\Unmuddle.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\Unmuddle.exe"; Description: "Start Unmuddle now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/IM Unmuddle.exe /F"; Flags: runhidden; RunOnceId: "StopUnmuddle"

[Code]
// The voice, the models and the settings live under the user's profile and are left alone on uninstall:
//   %LOCALAPPDATA%\Unmuddle\models   (the voice, about 350 MB, and any AI model or dictionary)
//   %APPDATA%\Unmuddle\settings.json

// A copy still running from the Helpers days would keep its folder from being removed.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM Helpers.App.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
