; Copyright © 2026 George Culache (george-gc-v01)
; SPDX-License-Identifier: GPL-3.0-only
[Setup]
AppId={{48D67042-29C9-4A04-9673-F13649B83B40}
AppName=Neon Orbit
AppVersion=0.1.0-beta
AppPublisher=George Culache
DefaultDirName={localappdata}\Programs\NeonOrbit
DefaultGroupName=Neon Orbit
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=Neon-Orbit-v0.1.0-beta-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
SetupIconFile=..\src\NeonOrbit.App\Assets\NeonOrbit.ico
UninstallDisplayIcon={app}\NeonOrbitProcess.v2026.exe
CloseApplications=yes
[Files]
Source: "..\artifacts\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Neon Orbit"; Filename: "{app}\NeonOrbitProcess.v2026.exe"
Name: "{group}\Uninstall Neon Orbit"; Filename: "{uninstallexe}"
[Run]
Filename: "{app}\NeonOrbitProcess.v2026.exe"; Description: "Open Neon Orbit"; Flags: nowait postinstall skipifsilent unchecked
