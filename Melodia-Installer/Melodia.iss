; ============================================================================
;  Melodia installer  -  Inno Setup 6.6 or newer (needed for "modern dark")
;  Build:  iscc Melodia.iss            (or  iscc /DMyAppVersion=1.2.0 Melodia.iss)
;  Input:  .\publish\   = output of `dotnet publish` (see build.ps1)
; ============================================================================

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppName      "Melodia"
#define MyAppExe       "Melodia.exe"
; Change the next two lines to your own name and the app's web page.
#define MyAppPublisher "Pacific"
#define MyAppURL       "https://github.com/kimpacific"

[Setup]
; Never change AppId after your first public release: it is how updates find the old install.
AppId={{7891B7F7-C72C-456C-8652-98D0BA191EA2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}

; --- Where it installs --------------------------------------------------------
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

; --- Look and feel ------------------------------------------------------------
; "modern dark" matches the app's dark theme. If your Inno version rejects it, update to 6.6+.
WizardStyle=modern dark
WizardSizePercent=110
WizardImageFile=assets\wizard-large.png
WizardSmallImageFile=assets\wizard-small.png
WizardImageBackColor=$141414
SetupIconFile=assets\logo.ico
UninstallDisplayIcon={app}\{#MyAppExe}
UninstallDisplayName={#MyAppName}

; --- Terms + Privacy (one acceptance page, user must accept to continue) -------
LicenseFile=docs\LICENSE_AND_PRIVACY.txt

; --- Output -------------------------------------------------------------------
OutputDir=output
OutputBaseFilename=Melodia-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
RestartApplications=no

; --- Code signing (uncomment after you register a tool named "signtool" in the IDE
;     or pass /Ssigntool=... on the iscc command line) ---------------------------
; SignTool=signtool
; SignedUninstaller=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to {#MyAppName}
WelcomeLabel2=This will install {#MyAppName} {#MyAppVersion} on your computer.%n%nA desktop music player with full-length streaming, local playlists, genre browsing and a 10-band equalizer.%n%nIt is recommended that you close other applications before continuing.
FinishedHeadingLabel=You're all set
FinishedLabel={#MyAppName} has been installed. Click Finish to close Setup.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
; Admin ("all users") installs go to Program Files, which normal users can't write to. LibVLC writes its plugin
; cache (plugins.dat) next to its plugins; if it can't, it rescans every plugin on EVERY launch (slow start).
Name: "{app}\libvlc"; Permissions: users-modify; Check: IsAdminInstallMode

[Files]
; Everything dotnet publish produced (app, .NET runtime, LibVLC audio engine and its plugins folder)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Copy of the legal text, so users can re-read it later from the Start menu
Source: "docs\LICENSE_AND_PRIVACY.txt"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autoprograms}\{#MyAppName} Terms and Privacy"; Filename: "{app}\docs\LICENSE_AND_PRIVACY.txt"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
; Builds LibVLC's plugin cache now (no window), so the very first launch is as fast as every later one.
Filename: "{app}\{#MyAppExe}"; Parameters: "--warm-audio"; StatusMsg: "Preparing the audio engine..."; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExe}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; plugins.dat is created by LibVLC after install, so the uninstaller doesn't know about it
Type: filesandordirs; Name: "{app}\libvlc"

[Code]
// Uninstall: the app keeps playlists, favorites and equalizer settings in %AppData%\Melodia.
// Keep them by default; only delete if the user explicitly says yes.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{userappdata}\{#MyAppName}');
    if DirExists(DataDir) and (not UninstallSilent) then
    begin
      if MsgBox('Also delete your playlists, favorites and equalizer settings?' + #13#10 +
                '(' + DataDir + ')' + #13#10#13#10 +
                'Choose No to keep them for a future install.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
