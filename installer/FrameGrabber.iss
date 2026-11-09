#ifndef AppVersion
  #error AppVersion must be supplied by build-installer.ps1
#endif
#ifndef PayloadRoot
  #error PayloadRoot must be supplied by build-installer.ps1
#endif
[Setup]
AppId={{B3A7C1E4-5D92-4F68-8E1A-2C6D90F7A354}
AppName=FrameGrabber
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher=Warm Butter Design
AppUpdatesURL=https://github.com/dgoldwas/FrameGrabber/releases
DefaultDirName={autopf}\FrameGrabber
UsePreviousAppDir=no
DefaultGroupName=FrameGrabber
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=FrameGrabber-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\FrameGrabber.ico
UninstallDisplayIcon={app}\FrameGrabber.exe
CloseApplications=yes

[Files]
; FFmpeg is deliberately not included. FrameGrabber finds it on PATH or beside the exe, or offers to install it with winget.
Source: "{#PayloadRoot}\FrameGrabber.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\FrameGrabber"; Filename: "{app}\FrameGrabber.exe"
Name: "{autodesktop}\FrameGrabber"; Filename: "{app}\FrameGrabber.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\FrameGrabber.exe"; Description: "Launch FrameGrabber"; Flags: nowait postinstall skipifsilent
